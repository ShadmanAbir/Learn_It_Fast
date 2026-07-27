using Lif.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace Lif.Api.Endpoints;

/// <summary>Maps Result errors to RFC 9457 problem+json with the stable errorCode (doc 12 §1).</summary>
internal static class ApiResults
{
    /// <summary>Phase-1 identity stand-in: the device-token JWT of doc 12 replaces this header later.</summary>
    public const string LearnerHeader = "X-Learner-Id";

    public static IResult Problem(Error error) => Results.Problem(
        title: error.Code,
        detail: error.Message,
        statusCode: StatusFor(error.Type),
        extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });

    public static IResult LearnerRequired() => Results.Problem(
        title: "auth.learner_required",
        detail: $"The {LearnerHeader} header with the learner id is required.",
        statusCode: StatusCodes.Status401Unauthorized,
        extensions: new Dictionary<string, object?> { ["errorCode"] = "auth.learner_required" });

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };
}
