using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace CustomerCrudApi.Logging;

public sealed class SqliteLoggerProvider : ILoggerProvider
{
    private readonly string _connectionString;
    private readonly Channel<LogEntry> _channel;
    private readonly Task _processTask;
    private readonly CancellationTokenSource _cts = new();

    public SqliteLoggerProvider(string connectionString)
    {
        _connectionString = connectionString;
        EnsureLogTable();
        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _processTask = Task.Run(ProcessLogQueueAsync);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SqliteLogger(_channel.Writer, categoryName);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();

        // Best-effort flush — wait briefly for the background writer to drain.
        _processTask.Wait(TimeSpan.FromSeconds(3));
        _cts.Dispose();
    }

    private void EnsureLogTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Logs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TimestampUtc TEXT NOT NULL,
    Level TEXT NOT NULL,
    Category TEXT NOT NULL,
    Message TEXT NOT NULL,
    Exception TEXT NULL
);";

        command.ExecuteNonQuery();
    }

    private async Task ProcessLogQueueAsync()
    {
        var batch = new List<LogEntry>();

        try
        {
            while (await _channel.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_channel.Reader.TryRead(out var entry))
                {
                    batch.Add(entry);
                    if (batch.Count >= 100) break;
                }

                if (batch.Count > 0)
                {
                    WriteBatch(batch);
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down — flush remaining entries.
        }

        while (_channel.Reader.TryRead(out var remaining))
        {
            batch.Add(remaining);
        }

        if (batch.Count > 0)
        {
            WriteBatch(batch);
        }
    }

    private void WriteBatch(List<LogEntry> entries)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO Logs (TimestampUtc, Level, Category, Message, Exception)
VALUES ($timestampUtc, $level, $category, $message, $exception);";

            var pTimestamp = command.Parameters.Add("$timestampUtc", SqliteType.Text);
            var pLevel = command.Parameters.Add("$level", SqliteType.Text);
            var pCategory = command.Parameters.Add("$category", SqliteType.Text);
            var pMessage = command.Parameters.Add("$message", SqliteType.Text);
            var pException = command.Parameters.Add("$exception", SqliteType.Text);

            foreach (var entry in entries)
            {
                pTimestamp.Value = entry.TimestampUtc;
                pLevel.Value = entry.Level;
                pCategory.Value = entry.Category;
                pMessage.Value = entry.Message;
                pException.Value = entry.Exception ?? (object)DBNull.Value;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            // Swallow logging errors to avoid crashing the application.
        }
    }

    private sealed class SqliteLogger : ILogger
    {
        private readonly ChannelWriter<LogEntry> _writer;
        private readonly string _categoryName;

        public SqliteLogger(ChannelWriter<LogEntry> writer, string categoryName)
        {
            _writer = writer;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            var entry = new LogEntry(
                DateTime.UtcNow.ToString("O"),
                logLevel.ToString(),
                _categoryName,
                message,
                exception?.ToString());

            // Fire-and-forget enqueue; drops if the channel is full.
            _writer.TryWrite(entry);
        }
    }

    private sealed record LogEntry(
        string TimestampUtc,
        string Level,
        string Category,
        string Message,
        string? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
