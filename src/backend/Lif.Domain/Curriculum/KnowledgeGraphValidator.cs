using Lif.SharedKernel.Results;

namespace Lif.Domain.Curriculum;

/// <summary>Authoring/seed-time validation of the knowledge graph (doc 08 §9).</summary>
public static class KnowledgeGraphValidator
{
    /// <summary>Topological sort over `requires` + `supports` edges; fails on any cycle.</summary>
    public static Result EnsureAcyclic(IReadOnlyList<Concept> concepts)
    {
        var ids = concepts.Select(c => c.Id).ToHashSet();
        var inDegree = concepts.ToDictionary(c => c.Id, _ => 0);
        var dependents = new Dictionary<Guid, List<Guid>>();

        foreach (var concept in concepts)
        {
            foreach (var dependency in concept.Dependencies)
            {
                if (!ids.Contains(dependency.PrerequisiteId))
                {
                    continue; // dangling references are reported by the seed validator, not here
                }

                inDegree[concept.Id]++;
                (dependents.TryGetValue(dependency.PrerequisiteId, out var list)
                    ? list
                    : dependents[dependency.PrerequisiteId] = []).Add(concept.Id);
            }
        }

        var queue = new Queue<Guid>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var visited = 0;
        while (queue.TryDequeue(out var current))
        {
            visited++;
            if (!dependents.TryGetValue(current, out var next))
            {
                continue;
            }

            foreach (var dependent in next)
            {
                if (--inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        return visited == concepts.Count
            ? Result.Success()
            : Result.Failure(CurriculumErrors.CycleDetected);
    }
}
