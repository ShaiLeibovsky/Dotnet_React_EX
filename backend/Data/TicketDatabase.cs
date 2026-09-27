using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TicketApi.Entities;
using TicketApi.Services;

namespace TicketApi.Data;

public static class TicketDatabase
{
    private static readonly JsonSerializerOptions SeedJsonOptions =
        new(JsonSerializerDefaults.Web);

    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var tickets = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var logger = scope
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(TicketDatabase).FullName!);
        var seedPath = scope
            .ServiceProvider.GetRequiredService<IOptions<TicketStoreOptions>>()
            .Value.SeedPath;

        await tickets.Database.MigrateAsync();

        if (await tickets.Tickets.AnyAsync())
            return;

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
        tickets.Tickets.AddRange(seeded);
        await tickets.SaveChangesAsync();
    }
}
