namespace TicketApi.Services;

/// <summary>Thrown for invalid input; mapped to a 400 ProblemDetails response.</summary>
public sealed class ValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public static ValidationException Single(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
