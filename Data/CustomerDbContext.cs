using CustomerCrudApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerCrudApi.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customerEntity = modelBuilder.Entity<Customer>();

        customerEntity.HasKey(c => c.Id);

        // Force Guid Id to be stored and compared as lowercase TEXT ("D" format).
        // Without this, EF Core's SQLite provider binds Guid parameters as BLOB,
        // which never matches TEXT values inserted by raw SQL (e.g. seed migrations).
        customerEntity.Property(c => c.Id)
            .HasConversion(
                g => g.ToString("D"),
                s => Guid.Parse(s));

        customerEntity.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        customerEntity.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        customerEntity.Property(c => c.Email).IsRequired().HasMaxLength(200);
        customerEntity.Property(c => c.Phone).IsRequired().HasMaxLength(30);
        customerEntity.Property(c => c.Address).IsRequired().HasMaxLength(300);

        customerEntity.HasIndex(c => c.Email).IsUnique();

        customerEntity.Property(c => c.DateOfBirth)
            .HasConversion(
                value => value.ToUniversalTime(),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        customerEntity.Property(c => c.CreatedAtUtc)
            .HasConversion(
                value => value.ToUniversalTime(),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        customerEntity.Property(c => c.UpdatedAtUtc)
            .HasConversion(
                value => value.ToUniversalTime(),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        base.OnModelCreating(modelBuilder);
    }
}
