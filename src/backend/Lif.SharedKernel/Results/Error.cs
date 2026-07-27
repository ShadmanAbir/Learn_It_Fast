namespace Lif.SharedKernel.Results;

/// <summary>Failure category; the API edge maps it to an HTTP status (RFC 9457).</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
}

/// <summary>A coded error. Codes are stable, machine-readable, and map to RFC 9457 problem details at the API edge.</summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
