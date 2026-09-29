using System.Reflection;
using Xunit.Sdk;
using TicketApi.Configuration;
using TicketApi.Modules.Tickets.Stores;

namespace TicketApi.Tests.Support;

/// <summary>Runs the test once per <see cref="ITicketStore"/> implementation.</summary>
public sealed class EveryTicketStoreAttribute : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod) =>
        Enum.GetValues<TicketStoreProvider>().Select(provider => new object[] { provider });
}
