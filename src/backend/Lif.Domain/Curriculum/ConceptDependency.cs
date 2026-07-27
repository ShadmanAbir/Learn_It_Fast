namespace Lif.Domain.Curriculum;

/// <summary>Directed edge: the owning concept depends on <paramref name="PrerequisiteId"/>.</summary>
public sealed record ConceptDependency(Guid PrerequisiteId, DependencyType Type, double MinMastery = 0.80);
