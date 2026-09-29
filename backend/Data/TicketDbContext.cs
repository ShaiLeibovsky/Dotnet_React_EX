using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TicketApi.Auth;
using TicketApi.Tickets;

namespace TicketApi.Data;

public sealed class TicketDbContext : DbContext
{
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTime =
        new(
            stored => stored.ToUniversalTime(),
            read => DateTime.SpecifyKind(read, DateTimeKind.Utc)
        );

    public TicketDbContext(DbContextOptions<TicketDbContext> options)
        : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<AdminUser> Admins => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(ticket =>
        {
            ticket.HasKey(t => t.Id);
            ticket.Property(t => t.Name).IsRequired();
            ticket.Property(t => t.Email).IsRequired();
            ticket.Property(t => t.Description).IsRequired();
            ticket.Property(t => t.Summary).IsRequired();
            ticket.Property(t => t.Status).IsRequired();
            ticket.Property(t => t.Resolution).IsRequired();
            ticket.Property(t => t.ImageUrl).IsRequired();
            ticket.Property(t => t.CreatedAt).HasConversion(UtcDateTime);
            ticket.Property(t => t.UpdatedAt).HasConversion(UtcDateTime);
        });

        modelBuilder.Entity<AdminUser>(admin =>
        {
            admin.HasKey(a => a.Id);
            admin.HasIndex(a => a.Email).IsUnique();
            admin.Property(a => a.Email).IsRequired();
            admin.Property(a => a.PasswordHash).IsRequired();
        });
    }
}
