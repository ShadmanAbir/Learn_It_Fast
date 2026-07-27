using Lif.Application.Abstractions;
using Lif.Domain.Learner;
using Lif.Domain.Scheduling;
using Lif.SharedKernel.Results;

namespace Lif.Application.Reviews;

/// <summary>POST /reviews/{itemId}/answers (doc 12 §4): FSRS rating 1–4.</summary>
public sealed record AnswerReviewCommand(Guid LearnerId, Guid ReviewItemId, int Rating, int? ElapsedMs = null);

public sealed record AnswerReviewResult(
    Guid ReviewItemId,
    DateTimeOffset NextDueAt,
    double Stability,
    string? WordStatus);

/// <summary>
/// Applies one review answer: FSRS reschedule + review log, and for word items the
/// word-status state machine (Known/Mastered/lapse tracking, doc 07 §3).
/// </summary>
public sealed class AnswerReviewHandler(
    IReviewItemRepository reviews,
    IWordStateRepository wordStates,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<AnswerReviewResult>> HandleAsync(
        AnswerReviewCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Rating is < 1 or > 4)
        {
            return Result.Failure<AnswerReviewResult>(ApplicationErrors.InvalidReviewRating);
        }

        var item = await reviews.GetAsync(command.ReviewItemId, cancellationToken);
        if (item is null || item.LearnerId != command.LearnerId)
        {
            return Result.Failure<AnswerReviewResult>(ApplicationErrors.ReviewItemNotFound);
        }

        var now = clock.UtcNow;
        var rating = (ReviewRating)command.Rating;
        var log = item.Answer(rating, now);
        reviews.AddLog(log);

        string? wordStatus = null;
        if (item.Kind == ReviewItemKind.Word)
        {
            var word = await wordStates.GetAsync(command.LearnerId, item.ItemRef, cancellationToken);
            if (word is null)
            {
                word = WordState.Meet(command.LearnerId, item.ItemRef, WordSource.Lesson);
                wordStates.Add(word);
            }

            word.ApplyReview(rating, item.State, now);
            wordStatus = word.Status.ToString();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AnswerReviewResult(item.Id, item.State.DueAt, item.State.Stability, wordStatus);
    }
}
