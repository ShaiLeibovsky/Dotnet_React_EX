using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TicketApi.Data;
using TicketApi.Endpoints;
using TicketApi.Entities;
using TicketApi.Services;

var builder = WebApplication.CreateBuilder(args);

// camelCase JSON to match the React frontend contract.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// Options.
builder.Services.Configure<TicketStoreOptions>(
    builder.Configuration.GetSection(TicketStoreOptions.SectionName)
);
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName)
);
builder.Services.Configure<AuthOptions>(
    builder.Configuration.GetSection(AuthOptions.SectionName)
);

// Application services.
var storeOptions =
    builder.Configuration.GetSection(TicketStoreOptions.SectionName).Get<TicketStoreOptions>()
    ?? new TicketStoreOptions();
var storeIsSqlite = storeOptions.Provider == TicketStoreProvider.Sqlite;

// ADR-0002 section 7, the generated development signing key.
var signingKeyIsGenerated = string.IsNullOrWhiteSpace(
    builder.Configuration[$"{AuthOptions.SectionName}:SigningKey"]
);
if (signingKeyIsGenerated)
    builder.Configuration[$"{AuthOptions.SectionName}:SigningKey"] =
        DevelopmentSigningKey.LoadOrCreate(
            Path.ChangeExtension(storeOptions.DatabasePath, ".signing-key")
        );

// ADR-0002 section 2, always SQLite.
builder.Services.AddDbContext<TicketDbContext>(options =>
    options.UseSqlite($"Data Source={storeOptions.DatabasePath}")
);

if (storeIsSqlite)
    builder.Services.AddScoped<ITicketStore, SqliteTicketStore>();
else
    builder.Services.AddSingleton<ITicketStore, JsonTicketStore>();

var emailOptions =
    builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()
    ?? new EmailOptions();

if (emailOptions.SmtpConfigured)
    builder.Services.AddSingleton<ICustomerNotifier, EmailNotifier>();
else
    builder.Services.AddSingleton<ICustomerNotifier, LogNotifier>();

builder.Services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddScoped<AdminAuthService>();
builder.Services.AddScoped<ITicketService, TicketService>();

var authOptions =
    builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
    ?? new AuthOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AuthOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = AuthOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = AdminAuthService.SigningKeyOf(authOptions),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        }
    );
builder.Services.AddAuthorization();

// Allow the Vite dev origin so the frontend can call the API directly.
var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (signingKeyIsGenerated)
    app.Logger.LogWarning(
        "No Auth:SigningKey configured; signing tokens with a generated key kept beside "
            + "the database. Set one through user-secrets or the environment to deploy."
    );

await TicketDatabase.MigrateAndSeedAsync(app.Services, seedTickets: storeIsSqlite);

app.Logger.LogInformation(
    "Customer notifications are delivered by {Notifier}.",
    app.Services.GetRequiredService<ICustomerNotifier>().GetType().Name
);

// Map ValidationException to a 400 ValidationProblem; everything else to 500.
app.UseExceptionHandler(handler =>
    handler.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (error is ValidationException validation)
        {
            await Results
                .ValidationProblem(validation.Errors)
                .ExecuteAsync(context);
            return;
        }

        await Results
            .Problem(statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(context);
    })
);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapTicketEndpoints();

app.Run();
