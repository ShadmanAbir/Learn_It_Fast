namespace Lif.Domain.Curriculum;

/// <summary>Edge type in the knowledge graph: <c>Requires</c> gates, <c>Supports</c> boosts (doc 08 §3).</summary>
public enum DependencyType
{
    Requires = 1,
    Supports = 2,
}
