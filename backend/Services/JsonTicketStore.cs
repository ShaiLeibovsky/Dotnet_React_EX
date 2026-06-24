using System.Text.Json;
using Microsoft.Extensions.Options;
using TicketApi.Entities;

namespace TicketApi.Services;

/// <summary>
/// File-backed <see cref="ITicketStore"/>. Keeps an in-memory copy guarded by a
/// <see cref="SemaphoreSlim"/> so concurrent requests can't corrupt the file
/// during read-modify-write cycles.
/// </summary>
public sealed class JsonTicketStore : ITicketStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private readonly string? _seedPath;
    private readonly ILogger<JsonTicketStore> _logger;
    private List<Ticket>? _cache;

    public JsonTicketStore(
        IOptions<TicketStoreOptions> options,
        ILogger<JsonTicketStore> logger
    )
    {
        _filePath = options.Value.FilePath;
        _seedPath = options.Value.SeedPath;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var tickets = await LoadAsync(ct);
            // Return a defensive copy so callers can't mutate the cache.
            return tickets.Select(Clone).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var tickets = await LoadAsync(ct);
            var found = tickets.FirstOrDefault(t => t.Id == id);
            return found is null ? null : Clone(found);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var tickets = await LoadAsync(ct);
            tickets.Insert(0, ticket);
            await SaveAsync(tickets, ct);
            return Clone(ticket);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Ticket?> UpdateAsync(
        string id,
        Action<Ticket> mutate,
        CancellationToken ct = default
    )
    {
        await _gate.WaitAsync(ct);
        try
        {
            var tickets = await LoadAsync(ct);
            var ticket = tickets.FirstOrDefault(t => t.Id == id);
            if (ticket is null)
                return null;

            mutate(ticket);
            ticket.UpdatedAt = DateTime.UtcNow;
            await SaveAsync(tickets, ct);
            return Clone(ticket);
        }
        finally
        {
            _gate.Release();
        }
    }

    // --- helpers (must be called while holding _gate) ---

    private async Task<List<Ticket>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null)
            return _cache;

        if (!File.Exists(_filePath))
            await SeedAsync(ct);

        if (File.Exists(_filePath))
        {
            await using var stream = File.OpenRead(_filePath);
            _cache =
                await JsonSerializer.DeserializeAsync<List<Ticket>>(
                    stream,
                    JsonOptions,
                    ct
                ) ?? [];
        }
        else
        {
            _cache = [];
        }

        return _cache;
    }

    private async Task SeedAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_seedPath) || !File.Exists(_seedPath))
        {
            _logger.LogInformation(
                "No seed file at {SeedPath}; starting with an empty store.",
                _seedPath
            );
            return;
        }

        _logger.LogInformation("Seeding ticket store from {SeedPath}.", _seedPath);
        var seedJson = await File.ReadAllTextAsync(_seedPath, ct);
        await File.WriteAllTextAsync(_filePath, seedJson, ct);
    }

    private async Task SaveAsync(List<Ticket> tickets, CancellationToken ct)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, tickets, JsonOptions, ct);
    }

    private static Ticket Clone(Ticket t) =>
        new()
        {
            Id = t.Id,
            Name = t.Name,
            Email = t.Email,
            Description = t.Description,
            Summary = t.Summary,
            Status = t.Status,
            Resolution = t.Resolution,
            Responses = t.Responses.Select(r => new TicketResponse
            {
                Id = r.Id,
                Author = r.Author,
                Role = r.Role,
                Body = r.Body,
                CreatedAt = r.CreatedAt,
            })
                .ToList(),
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
        };

    public void Dispose() => _gate.Dispose();
}
