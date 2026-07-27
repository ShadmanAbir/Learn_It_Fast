using Lif.Domain.Curriculum;
using Lif.SharedKernel.Primitives;

namespace Lif.Application.Abstractions;

/// <summary>Read-only access to published curriculum content (static; never mutated by learners).</summary>
public interface ICurriculumReader
{
    /// <summary>Latest published version for a language, or null when none is available.</summary>
    public Task<CurriculumVersion?> GetActiveVersionAsync(LanguageCode language, CancellationToken cancellationToken = default);

    /// <summary>All concepts of a curriculum version (the full knowledge graph slice).</summary>
    public Task<IReadOnlyList<Concept>> GetConceptsAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>Unit id → order index, for frontier ranking.</summary>
    public Task<IReadOnlyDictionary<Guid, int>> GetUnitOrderAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>Curated exercises targeting any of the given concepts (ADR-008 fallback bank).</summary>
    public Task<IReadOnlyList<CuratedExercise>> GetExercisesForConceptsAsync(
        Guid versionId, IReadOnlyCollection<Guid> conceptIds, CancellationToken cancellationToken = default);
}
