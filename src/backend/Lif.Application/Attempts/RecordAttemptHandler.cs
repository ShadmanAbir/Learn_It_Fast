using Lif.Application.Abstractions;
using Lif.Domain.Learner;
using Lif.Domain.Learning;
using Lif.Domain.Scheduling;
using Lif.SharedKernel.Primitives;
using Lif.SharedKernel.Results;

namespace Lif.Application.Attempts;

/// <summary>POST /exercises/{id}/attempts (doc 12 §3): evaluate + feedback in one round-trip.</summary>
public sealed record RecordAttemptCommand(
    Guid LearnerId,
    Guid ExerciseInstanceId,
    string Response,
    int HintsUsed = 0,
    int? LatencyMs = null);

public sealed record MasteryDeltaDto(Guid ConceptId, double From, double To, bool NeedsRemediation);

public sealed record AttemptResultDto(
    Guid AttemptId,
    double Outcome,
    string EvaluatedBy,
    string CorrectAnswer,
    string? Explanation,
    IReadOnlyList<MasteryDeltaDto> MasteryDeltas);

/// <summary>
/// Records one attempt: deterministic evaluation in the session aggregate, then BKT mastery
/// updates per concept, mistake logging (never silently dropped), and first-exposure review
/// scheduling — the full FR-EXR-3 feedback loop without any LLM in the path.
/// </summary>
public sealed class RecordAttemptHandler(
    IStudySessionRepository sessions,
    IConceptMasteryRepository masteries,
    IMistakeRepository mistakes,
    IReviewItemRepository reviews,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    /// <summary>Error-taxonomy fallback when a curated exercise carries no code (kept explicit, doc 06 §3.2).</summary>
    public const string UnmappedErrorCode = "UNSPECIFIED";

    public async Task<Result<AttemptResultDto>> HandleAsync(
        RecordAttemptCommand command, CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetByExerciseAsync(command.ExerciseInstanceId, cancellationToken);
        if (session is null || session.LearnerId != command.LearnerId)
        {
            return Result.Failure<AttemptResultDto>(ApplicationErrors.ExerciseNotFound);
        }

        var now = clock.UtcNow;
        var attemptResult = session.RecordAttempt(
            command.ExerciseInstanceId, command.Response, command.HintsUsed, command.LatencyMs, now);
        if (attemptResult.IsFailure)
        {
            return Result.Failure<AttemptResultDto>(attemptResult.Error);
        }

        var attempt = attemptResult.Value;
        var spec = session.Activities
            .SelectMany(a => a.Exercises)
            .First(e => e.Id == command.ExerciseInstanceId)
            .Spec;

        var deltas = await ApplyMasteryAsync(command, spec, attempt.Outcome, now, cancellationToken);

        if (attempt.Outcome < 0.5)
        {
            mistakes.Add(MistakeRecord.Log(
                command.LearnerId,
                Skill.Grammar,
                spec.ErrorCode ?? UnmappedErrorCode,
                spec.ConceptIds.Count > 0 ? spec.ConceptIds[0] : null,
                MistakeSource.Exercise,
                command.ExerciseInstanceId,
                now,
                wrong: command.Response,
                right: spec.AnswerKey));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AttemptResultDto(
            attempt.Id, attempt.Outcome, attempt.EvaluatedBy, spec.AnswerKey, spec.Explanation, deltas);
    }

    /// <summary>BKT update per tested concept + first-exposure review scheduling (doc 08 §6/§7).</summary>
    private async Task<IReadOnlyList<MasteryDeltaDto>> ApplyMasteryAsync(
        RecordAttemptCommand command, ExerciseSpec spec, double outcome, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var guess = spec.Type == ExerciseType.MultipleChoice
            ? MasteryUpdater.GuessForMultipleChoice(spec.Options.Count)
            : MasteryUpdater.DefaultGuess;

        var deltas = new List<MasteryDeltaDto>();
        foreach (var conceptId in spec.ConceptIds)
        {
            var mastery = await masteries.GetAsync(command.LearnerId, conceptId, cancellationToken);
            if (mastery is null)
            {
                mastery = ConceptMastery.Start(command.LearnerId, conceptId);
                masteries.Add(mastery);
            }

            var from = mastery.Mastery;
            mastery.ApplyObservation(outcome, command.HintsUsed, guess, MasteryUpdater.DefaultSlip, now);
            deltas.Add(new MasteryDeltaDto(conceptId, from, mastery.Mastery, mastery.NeedsRemediation));

            // First practice puts the concept into the unified review queue (doc 08 §7).
            var item = await reviews.FindAsync(command.LearnerId, ReviewItemKind.ConceptDrill, conceptId, cancellationToken);
            if (item is null)
            {
                reviews.Add(ReviewItem.Introduce(command.LearnerId, ReviewItemKind.ConceptDrill, conceptId, now));
            }
        }

        return deltas;
    }
}
