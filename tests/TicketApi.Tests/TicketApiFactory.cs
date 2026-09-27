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

    public RecordingEmailService Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "TicketStore:FilePath",
            Path.Combine(storeDirectory, "tickets.json")
        );
        builder.UseSetting(
            "TicketStore:SeedPath",
            Path.Combine(AppContext.BaseDirectory, "dataset.json")
        );

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(storeDirectory))
            Directory.Delete(storeDirectory, recursive: true);
    }
}
