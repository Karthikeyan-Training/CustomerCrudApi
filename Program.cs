using CustomerCrudApi.Data;
using CustomerCrudApi.Repositories;
using CustomerCrudApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CustomerDbContext>(options =>
{
    var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=customers.db";

    // Resolve any relative Data Source path against the content root so the DB file
    // is always created next to the project, regardless of the process working directory
    // (bin/Debug/netX.Y/, dotnet run from a parent directory, IDE launch, etc.).
    var connectionString = ResolveConnectionString(rawConnectionString, builder.Environment.ContentRootPath);

    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<ICustomerRepository, SqliteCustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Rewrites "Data Source=filename.db" to an absolute path anchored at contentRoot.
// Leaves absolute paths (e.g. "Data Source=C:\..." or ":memory:") untouched.
static string ResolveConnectionString(string connectionString, string contentRootPath)
{
    const string dataSourceKey = "Data Source=";
    var index = connectionString.IndexOf(dataSourceKey, StringComparison.OrdinalIgnoreCase);
    if (index < 0)
    {
        return connectionString;
    }

    var afterKey = connectionString.Substring(index + dataSourceKey.Length);
    var end = afterKey.IndexOf(';');
    var dataSource = end >= 0 ? afterKey.Substring(0, end) : afterKey;

    // Skip already-absolute paths and special SQLite URIs.
    if (Path.IsPathRooted(dataSource) || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || dataSource == ":memory:")
    {
        return connectionString;
    }

    var absoluteDataSource = Path.Combine(contentRootPath, dataSource);
    return connectionString.Substring(0, index)
        + dataSourceKey
        + absoluteDataSource
        + (end >= 0 ? afterKey.Substring(end) : string.Empty);
}
