using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TicketApi.Dtos;
using TicketApi.Services;

namespace TicketApi.Tests;

internal sealed class TicketApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "test-admin-password";

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

    /// <summary>A client carrying a bearer token for the seeded admin account.</summary>
    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = AdminEmail, password = AdminPassword }
        );
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<AdminSessionDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session!.Token
        );
        return client;
    }

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
        builder.UseSetting("Auth:SigningKey", "test-signing-key-at-least-32-bytes-long!!");
        builder.UseSetting("Auth:AdminEmail", AdminEmail);
        builder.UseSetting("Auth:AdminPassword", AdminPassword);
        builder.UseSetting(
            "TicketStore:SeedPath",
            Path.Combine(AppContext.BaseDirectory, "dataset.json")
        );

        ClearInheritedSmtpCredentials(builder);

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

    private static void ClearInheritedSmtpCredentials(IWebHostBuilder builder)
    {
        builder.UseSetting("Email:SmtpHost", null);
        builder.UseSetting("Email:SmtpUser", null);
        builder.UseSetting("Email:SmtpPassword", null);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(storeDirectory))
            Directory.Delete(storeDirectory, recursive: true);
    }
}
