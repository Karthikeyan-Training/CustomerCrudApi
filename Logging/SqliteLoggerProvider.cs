using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace CustomerCrudApi.Logging;

public sealed class SqliteLoggerProvider : ILoggerProvider
{
    private readonly string _connectionString;

    public SqliteLoggerProvider(string connectionString)
    {
        _connectionString = connectionString;
        EnsureLogTable();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SqliteLogger(_connectionString, categoryName);
    }

    public void Dispose()
    {
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

    private sealed class SqliteLogger : ILogger
    {
        private readonly string _connectionString;
        private readonly string _categoryName;

        public SqliteLogger(string connectionString, string categoryName)
        {
            _connectionString = connectionString;
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

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO Logs (TimestampUtc, Level, Category, Message, Exception)
VALUES ($timestampUtc, $level, $category, $message, $exception);";
            command.Parameters.AddWithValue("$timestampUtc", DateTime.UtcNow.ToString("O"));
            command.Parameters.AddWithValue("$level", logLevel.ToString());
            command.Parameters.AddWithValue("$category", _categoryName);
            command.Parameters.AddWithValue("$message", message);
            command.Parameters.AddWithValue("$exception", exception?.ToString() ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
