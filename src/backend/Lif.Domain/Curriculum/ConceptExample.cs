namespace Lif.Domain.Curriculum;

/// <summary>Curated example sentence for a concept.</summary>
public sealed record ConceptExample(string German, string English, string? Note, int OrderIndex);
