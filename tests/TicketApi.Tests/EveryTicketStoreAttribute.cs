using System.Reflection;
using Xunit.Sdk;

namespace TicketApi.Tests;

/// <summary>Runs the test once per <see cref="ITicketStore"/> implementation.</summary>
public sealed class EveryTicketStoreAttribute : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo testMethod) =>
        Enum.GetValues<TicketStoreProvider>().Select(provider => new object[] { provider });
}
