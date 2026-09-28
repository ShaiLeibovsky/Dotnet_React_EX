using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TicketApi.Services;

namespace TicketApi.Tests;

internal sealed class TicketApiFactory : WebApplicationFactory<Program>
{
    private readonly string storeDirectory =
        Directory.CreateTempSubdirectory("ticket-api-tests").FullName;
    private readonly TicketStoreProvider provider;

    public TicketApiFactory(TicketStoreProvider provider = TicketStoreProvider.Json)
    {
        this.provider = provider;
    }

    public RecordingNotifier Notifier { get; } = new();

    public Dictionary<string, string?> ConfigurationOverrides { get; } = [];

    public bool KeepConfiguredNotifier { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("TicketStore:Provider", provider.ToString());
        builder.UseSetting(
            "TicketStore:DatabasePath",
            Path.Combine(storeDirectory, "tickets.db")
        );
        builder.UseSetting(
            "TicketStore:FilePath",
            Path.Combine(storeDirectory, "tickets.json")
        );
        builder.UseSetting(
            "TicketStore:SeedPath",
            Path.Combine(AppContext.BaseDirectory, "dataset.json")
        );

        foreach (var (key, value) in ConfigurationOverrides)
            builder.UseSetting(key, value);

        if (KeepConfiguredNotifier)
            return;

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICustomerNotifier>();
            services.AddSingleton<ICustomerNotifier>(Notifier);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(storeDirectory))
            Directory.Delete(storeDirectory, recursive: true);
    }
}
