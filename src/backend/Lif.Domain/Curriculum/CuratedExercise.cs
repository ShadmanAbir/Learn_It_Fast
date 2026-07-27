using Lif.Domain.Learning;
using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Results;

namespace Lif.Domain.Curriculum;

/// <summary>
/// Curated exercise bank entry (ADR-008: curated fallbacks are mandatory). Versioned content
/// keyed by a stable code; the planner instantiates it into sessions as an
/// <see cref="ExerciseInstance"/> with <see cref="ExerciseSource.Curated"/>.
/// </summary>
public sealed class CuratedExercise : AggregateRoot
{
    private CuratedExercise()
    {
    }

    private CuratedExercise(Guid id, Guid versionId, string code, ExerciseSpec spec)
        : base(id)
    {
        VersionId = versionId;
        Code = code;
        Spec = spec;
    }

    public Guid VersionId { get; private set; }

    /// <summary>Stable key (e.g. <c>ex.a1.greeting.mc-morning</c>).</summary>
    public string Code { get; private set; } = string.Empty;

    public ExerciseSpec Spec { get; private set; } = null!;

    public static Result<CuratedExercise> Create(Guid versionId, string code, ExerciseSpec spec)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<CuratedExercise>(CurriculumErrors.ExerciseCodeRequired);
        }

        if (spec.ConceptIds.Count == 0)
        {
            return Result.Failure<CuratedExercise>(CurriculumErrors.ExerciseConceptRequired);
        }

        return new CuratedExercise(Guid.CreateVersion7(), versionId, code.Trim(), spec);
    }
}
