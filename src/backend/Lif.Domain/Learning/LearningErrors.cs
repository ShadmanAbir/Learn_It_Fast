using Lif.SharedKernel.Results;

namespace Lif.Domain.Learning;

/// <summary>Stable error catalog for the Learning context.</summary>
public static class LearningErrors
{
    public static readonly Error SessionNotPlanned =
        Error.Conflict("learning.session_not_planned", "Only a planned session can be started.");

    public static readonly Error SessionNotActive =
        Error.Conflict("learning.session_not_active", "The session is not active.");

    public static readonly Error ExerciseNotFound =
        Error.NotFound("learning.exercise_not_found", "The exercise does not belong to this session.");

    public static readonly Error ActivityNotFound =
        Error.NotFound("learning.activity_not_found", "The activity does not belong to this session.");

    public static readonly Error SessionAlreadyClosed =
        Error.Conflict("learning.session_already_closed", "The session is already completed or abandoned.");
}
