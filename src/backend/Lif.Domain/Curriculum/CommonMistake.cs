namespace Lif.Domain.Curriculum;

/// <summary>Curated common mistake; <paramref name="ErrorCode"/> links to the error taxonomy.</summary>
public sealed record CommonMistake(string Wrong, string Right, string Explanation, string ErrorCode);
