using System.ComponentModel.DataAnnotations;
using TicketApi.Dtos;
using TicketApi.Entities;

namespace TicketApi.Services;

public sealed class TicketService : ITicketService
{
    private readonly ITicketStore _store;
    private readonly ICustomerNotifier _notifier;
    private readonly ISummaryService _summaries;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        ITicketStore store,
        ICustomerNotifier notifier,
        ISummaryService summaries,
        ILogger<TicketService> logger
    )
    {
        _store = store;
        _notifier = notifier;
        _summaries = summaries;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tickets = await _store.GetAllAsync(ct);
        return tickets.Select(t => t.ToDto()).ToList();
    }

    public async Task<TicketDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var ticket = await _store.GetByIdAsync(id, ct);
        return ticket?.ToDto();
    }

    public async Task<TicketDto> CreateAsync(
        CreateTicketRequest request,
        CancellationToken ct = default
    )
    {
        Validate(request);

        var now = DateTime.UtcNow;
        var description = request.Description.Trim();
        var summary = await SummariseOrEmptyAsync(description, ct);
        var ticket = new Ticket
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Description = description,
            Summary = summary,
            Status = TicketStatuses.New,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _store.CreateAsync(ticket, ct);
        await _notifier.SendTicketCreatedAsync(created, ct);
        return created.ToDto();
    }

    public async Task<TicketDto?> UpdateAsync(
        string id,
        UpdateTicketRequest request,
        string? handlingAdminEmail,
        CancellationToken ct = default
    )
    {
        Validate(request);

        var existing = await _store.GetByIdAsync(id, ct);
        if (existing is null)
            return null;

        var previousStatus = existing.Status;
        var previousResolution = existing.Resolution;
        var newResolution = request.Resolution ?? string.Empty;

        var updated = await _store.UpdateAsync(
            id,
            t =>
            {
                t.Status = request.Status;
                t.Resolution = newResolution;
            },
            ct
        );

        if (updated is null)
            return null;

        if (updated.Status != previousStatus)
            await _notifier.SendStatusChangedAsync(
                updated,
                previousStatus,
                handlingAdminEmail,
                ct
            );
        if (updated.Resolution != previousResolution)
            await _notifier.SendResolutionChangedAsync(updated, handlingAdminEmail, ct);

        return updated.ToDto();
    }

    private async Task<string> SummariseOrEmptyAsync(string description, CancellationToken ct)
    {
        try
        {
            return await _summaries.SummariseAsync(description, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception failure)
        {
            _logger.LogWarning(
                failure,
                "Summary generation failed; creating the ticket without a summary."
            );
            return string.Empty;
        }
    }

    // --- validation ---

    private static void Validate(object request)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, context, results, validateAllProperties: true))
        {
            var errors = results
                .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty),
                    (r, member) => (member, r.ErrorMessage ?? "Invalid value"))
                .GroupBy(x => x.member, x => x.Item2)
                .ToDictionary(g => g.Key, g => g.ToArray());
            throw new ValidationException(errors);
        }

        if (request is UpdateTicketRequest update && !TicketStatuses.IsValid(update.Status))
            throw ValidationException.Single(
                nameof(update.Status),
                $"Status must be one of: {string.Join(", ", TicketStatuses.All)}."
            );
    }
}
