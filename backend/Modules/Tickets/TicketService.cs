using System.ComponentModel.DataAnnotations;
using ValidationException = TicketApi.Shared.ValidationException;
using TicketApi.Modules.Notifications.Util;
using TicketApi.Modules.Summaries.Util;
using TicketApi.Modules.Tickets.Dto;
using TicketApi.Modules.Tickets.Entities;
using TicketApi.Modules.Tickets.Stores;

namespace TicketApi.Modules.Tickets;

public sealed class TicketService : ITicketService
{
    private readonly ITicketStore _store;
    private readonly NotificationQueue _notificationQueue;
    private readonly SummaryQueue _summaryQueue;

    public TicketService(
        ITicketStore store,
        NotificationQueue notificationQueue,
        SummaryQueue summaryQueue
    )
    {
        _store = store;
        _notificationQueue = notificationQueue;
        _summaryQueue = summaryQueue;
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
        var ticket = new Ticket
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Description = request.Description.Trim(),
            Status = TicketStatuses.New,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _store.CreateAsync(ticket, ct);
        // ADR-0005 section 1, notifying after the response.
        _notificationQueue.EnqueueTicketCreated(created);
        _summaryQueue.Enqueue(created.Id);
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
            _notificationQueue.EnqueueStatusChanged(updated, previousStatus, handlingAdminEmail);
        if (updated.Resolution != previousResolution)
            _notificationQueue.EnqueueResolutionChanged(updated, handlingAdminEmail);

        return updated.ToDto();
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
