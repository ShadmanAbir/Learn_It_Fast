using Lif.SharedKernel.Results;

namespace Lif.Domain.Curriculum;

/// <summary>Stable error catalog for the Curriculum context.</summary>
public static class CurriculumErrors
{
    public static readonly Error ConceptCodeRequired =
        Error.Validation("curriculum.concept_code_required", "A concept requires a non-empty stable code.");

    public static readonly Error MasteryThresholdOutOfRange =
        Error.Validation("curriculum.mastery_threshold_out_of_range", "Mastery threshold must be within [0.5, 0.95].");

    public static readonly Error EstimatedMinutesInvalid =
        Error.Validation("curriculum.estimated_minutes_invalid", "Estimated minutes must be positive.");

    public static readonly Error SelfDependency =
        Error.Validation("curriculum.self_dependency", "A concept cannot depend on itself.");

    public static readonly Error DuplicateDependency =
        Error.Conflict("curriculum.duplicate_dependency", "The dependency already exists.");

    public static readonly Error CycleDetected =
        Error.Validation("curriculum.cycle_detected", "The concept dependency graph must be acyclic.");

    public static readonly Error LemmaRequired =
        Error.Validation("curriculum.lemma_required", "A vocabulary entry requires a non-empty lemma.");

    public static readonly Error ExerciseCodeRequired =
        Error.Validation("curriculum.exercise_code_required", "A curated exercise requires a non-empty stable code.");

    public static readonly Error ExerciseConceptRequired =
        Error.Validation("curriculum.exercise_concept_required", "A curated exercise must target at least one concept.");
}
