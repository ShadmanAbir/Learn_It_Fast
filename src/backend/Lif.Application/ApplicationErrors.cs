using Lif.SharedKernel.Results;

namespace Lif.Application;

/// <summary>Stable error catalog for application use cases (maps to RFC 9457 at the API edge).</summary>
public static class ApplicationErrors
{
    public static readonly Error LearnerNotFound =
        Error.NotFound("learner.not_found", "The learner does not exist.");

    public static readonly Error InvalidTargetLevel =
        Error.Validation("learner.invalid_target_level", "The target level is not a valid CEFR level.");

    public static readonly Error CurriculumNotAvailable =
        Error.NotFound("curriculum.not_available", "No published curriculum version is available for this language.");

    public static readonly Error SessionNotFound =
        Error.NotFound("session.not_found", "The session does not exist for this learner.");

    public static readonly Error SessionAlreadyOpen =
        Error.Conflict("session.already_open", "The learner already has an open session; complete or abandon it first.");

    public static readonly Error ExerciseNotFound =
        Error.NotFound("exercise.not_found", "The exercise does not exist in any open session of this learner.");

    public static readonly Error ReviewItemNotFound =
        Error.NotFound("review.item_not_found", "The review item does not exist for this learner.");

    public static readonly Error InvalidReviewRating =
        Error.Validation("review.invalid_rating", "The review rating must be 1 (Again), 2 (Hard), 3 (Good) or 4 (Easy).");
}
