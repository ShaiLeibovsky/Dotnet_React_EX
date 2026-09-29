using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TicketApi.Configuration;
using TicketApi.Modules.Auth;
using TicketApi.Modules.Auth.Entities;
using TicketApi.Modules.Tickets.Entities;

namespace TicketApi.Data;

public static class TicketDatabase
{
    private static readonly JsonSerializerOptions SeedJsonOptions =
        new(JsonSerializerDefaults.Web);

    public static async Task MigrateAndSeedAsync(IServiceProvider services, bool seedTickets)
    {
        using var scope = services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var logger = scope
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(TicketDatabase).FullName!);

        await database.Database.MigrateAsync();
        await SeedAdminAsync(scope.ServiceProvider, database, logger);

        if (seedTickets)
            await SeedTicketsAsync(scope.ServiceProvider, database, logger);
    }

    private static async Task SeedAdminAsync(
        IServiceProvider services,
        TicketDbContext database,
        ILogger logger
    )
    {
        var authOptions = services.GetRequiredService<IOptions<AuthOptions>>().Value;

        if (string.IsNullOrWhiteSpace(authOptions.AdminEmail) || string.IsNullOrWhiteSpace(authOptions.AdminPassword))
        {
            logger.LogWarning(
                "No Auth:AdminEmail / Auth:AdminPassword configured; no admin account seeded. "
                    + "Set both through user-secrets to sign in."
            );
            return;
        }

        var email = AdminAuthService.NormalizedEmail(authOptions.AdminEmail);
        if (await database.Admins.AnyAsync(existing => existing.Email == email))
            return;

        var admin = new AdminUser { Email = email };
        admin.PasswordHash = services
            .GetRequiredService<IPasswordHasher<AdminUser>>()
            .HashPassword(admin, authOptions.AdminPassword);

        database.Admins.Add(admin);
        await database.SaveChangesAsync();
        logger.LogInformation("Seeded the admin account {Email}.", admin.Email);
    }

    private static async Task SeedTicketsAsync(
        IServiceProvider services,
        TicketDbContext database,
        ILogger logger
    )
    {
        if (await database.Tickets.AnyAsync())
            return;

        var seedPath = services
            .GetRequiredService<IOptions<TicketStoreOptions>>()
            .Value.SeedPath;

        if (string.IsNullOrWhiteSpace(seedPath) || !File.Exists(seedPath))
        {
            logger.LogWarning(
                "No seed file at {SeedPath}; starting with an empty database.",
                seedPath
            );
            return;
        }

        logger.LogInformation("Seeding ticket database from {SeedPath}.", seedPath);
        await using var seedStream = File.OpenRead(seedPath);
        var seeded =
            await JsonSerializer.DeserializeAsync<List<Ticket>>(seedStream, SeedJsonOptions)
            ?? [];
        database.Tickets.AddRange(seeded);
        await database.SaveChangesAsync();
    }
}
