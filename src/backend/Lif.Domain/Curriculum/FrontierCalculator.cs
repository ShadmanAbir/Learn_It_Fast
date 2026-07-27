using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Curriculum;

/// <summary>A concept eligible for study, with the ranking signals that ordered it.</summary>
public sealed record FrontierEntry(Guid ConceptId, string Code, int CefrDistance, double Importance, int UnitOrder, int UnlockCount);

/// <summary>
/// Pure frontier computation (doc 08 §5): concepts whose hard prerequisites are met
/// but which are not yet mastered, ranked deterministically. The LLM never touches this.
/// </summary>
public static class FrontierCalculator
{
    /// <param name="concepts">All concepts of the active curriculum version.</param>
    /// <param name="mastery">Learner mastery per concept id; missing key = 0.</param>
    /// <param name="learnerLevel">Current (sub-)level used for CEFR-distance ranking.</param>
    /// <param name="unitOrder">Unit id → order index, for curriculum coherence ranking.</param>
    public static IReadOnlyList<FrontierEntry> Compute(
        IReadOnlyList<Concept> concepts,
        IReadOnlyDictionary<Guid, double> mastery,
        CefrLevel learnerLevel,
        IReadOnlyDictionary<Guid, int> unitOrder)
    {
        // Out-degree over `requires` edges: how many concepts each node unlocks.
        var unlockCount = new Dictionary<Guid, int>();
        foreach (var dependency in concepts.SelectMany(c => c.Requires))
        {
            unlockCount[dependency.PrerequisiteId] = unlockCount.GetValueOrDefault(dependency.PrerequisiteId) + 1;
        }

        return concepts
            .Where(c => MasteryOf(c.Id) < c.MasteryThreshold)
            .Where(c => c.Requires.All(d => MasteryOf(d.PrerequisiteId) >= d.MinMastery))
            .Select(c => new FrontierEntry(
                c.Id,
                c.Code,
                Math.Abs(c.Level.Rank - learnerLevel.Rank),
                c.Importance,
                c.UnitId is { } unitId ? unitOrder.GetValueOrDefault(unitId, int.MaxValue) : int.MaxValue,
                unlockCount.GetValueOrDefault(c.Id)))
            .OrderBy(e => e.CefrDistance)
            .ThenByDescending(e => e.Importance)
            .ThenBy(e => e.UnitOrder)
            .ThenByDescending(e => e.UnlockCount)
            .ThenBy(e => e.Code, StringComparer.Ordinal) // total order → deterministic plans
            .ToList();

        double MasteryOf(Guid conceptId) => mastery.GetValueOrDefault(conceptId);
    }
}
