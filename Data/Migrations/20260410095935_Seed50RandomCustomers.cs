using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerCrudApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class Seed50RandomCustomers : Migration
    {
        private const string SeedMarker = "__MIGRATION_SEED_20260410_RANDOM50__";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                WITH RECURSIVE seq(n) AS (
                    SELECT 1
                    UNION ALL
                    SELECT n + 1 FROM seq WHERE n < 50
                )
                INSERT INTO "Customers"
                (
                    "Id",
                    "FirstName",
                    "LastName",
                    "Email",
                    "Phone",
                    "DateOfBirth",
                    "Address",
                    "CreatedAtUtc",
                    "UpdatedAtUtc"
                )
                SELECT
                    lower(substr(hex(randomblob(16)), 1, 8) || '-' ||
                          substr(hex(randomblob(16)), 9, 4) || '-' ||
                          substr(hex(randomblob(16)), 13, 4) || '-' ||
                          substr(hex(randomblob(16)), 17, 4) || '-' ||
                          substr(hex(randomblob(16)), 21, 12)),
                    CASE abs(random()) % 10
                        WHEN 0 THEN 'Aarav'
                        WHEN 1 THEN 'Diya'
                        WHEN 2 THEN 'Kunal'
                        WHEN 3 THEN 'Meera'
                        WHEN 4 THEN 'Rahul'
                        WHEN 5 THEN 'Sneha'
                        WHEN 6 THEN 'Vikram'
                        WHEN 7 THEN 'Ananya'
                        WHEN 8 THEN 'Nikhil'
                        ELSE 'Priya'
                    END,
                    CASE abs(random()) % 10
                        WHEN 0 THEN 'Sharma'
                        WHEN 1 THEN 'Nair'
                        WHEN 2 THEN 'Verma'
                        WHEN 3 THEN 'Iyer'
                        WHEN 4 THEN 'Kapoor'
                        WHEN 5 THEN 'Patel'
                        WHEN 6 THEN 'Rao'
                        WHEN 7 THEN 'Singh'
                        WHEN 8 THEN 'Das'
                        ELSE 'Menon'
                    END,
                    'seed50_' || n || '_' || lower(hex(randomblob(4))) || '@example.com',
                    '+91-9' || printf('%09d', abs(random()) % 1000000000),
                    date('now', '-' || (7300 + (abs(random()) % 7300)) || ' days'),
                    '{SeedMarker}_' || n,
                    datetime('now'),
                    datetime('now')
                FROM seq;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM \"Customers\" WHERE \"Address\" LIKE '{SeedMarker}_%';");
        }
    }
}
