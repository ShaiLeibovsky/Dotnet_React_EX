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
    private readonly ISummaryService? summaries;

    public TicketApiFactory(
        TicketStoreProvider provider = TicketStoreProvider.Json,
        ISummaryService? summaries = null
    )
    {
        this.provider = provider;
        this.summaries = summaries;
    }

    public RecordingEmailService Emails { get; } = new();

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
        builder.UseSetting("Summary:ApiKey", string.Empty);
        builder.UseSetting(
            "TicketStore:SeedPath",
            Path.Combine(AppContext.BaseDirectory, "dataset.json")
        );

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);

            if (summaries is not null)
            {
                services.RemoveAll<ISummaryService>();
                services.AddSingleton<ISummaryService>(summaries);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(storeDirectory))
            Directory.Delete(storeDirectory, recursive: true);
    }
}
