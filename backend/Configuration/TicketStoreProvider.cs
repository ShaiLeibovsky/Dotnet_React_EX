using TicketApi.Modules.Tickets.Stores;

namespace TicketApi.Configuration;

/// <summary>Selects which <see cref="ITicketStore"/> implementation serves requests.</summary>
public enum TicketStoreProvider
{
    Json,
    Sqlite,
}
