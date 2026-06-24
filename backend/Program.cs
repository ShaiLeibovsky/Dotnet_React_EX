using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using TicketApi.Endpoints;
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

// Application services.
builder.Services.AddSingleton<ITicketStore, JsonTicketStore>();
builder.Services.AddSingleton<IEmailService, ConsoleEmailService>();
builder.Services.AddScoped<ITicketService, TicketService>();

// Allow the Vite dev origin so the frontend can call the API directly.
var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

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

app.MapTicketEndpoints();

app.Run();
