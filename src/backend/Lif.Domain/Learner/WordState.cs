using Lif.Domain.Scheduling;
using Lif.SharedKernel.Domain;

namespace Lif.Domain.Learner;

/// <summary>Lifecycle status of a word for a learner (doc 07 §3).</summary>
public enum WordStatus
{
    New = 1,
    Learning = 2,
    Known = 3,
    Mastered = 4,
    Forgotten = 5,
    Relearning = 6,
}

/// <summary>Where the learner first met the word.</summary>
public enum WordSource
{
    Lesson = 1,
    Reading = 2,
    Conversation = 3,
    Manual = 4,
}

/// <summary>A word crossed the mastered stability threshold.</summary>
public sealed record WordMastered(Guid LearnerId, Guid VocabEntryId, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>A previously known word lapsed back into relearning.</summary>
public sealed record WordLapsed(Guid LearnerId, Guid VocabEntryId, int LapseCount, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// Per-learner vocabulary state (doc 06 §3.2). Status transitions follow the
/// FSRS-driven state machine — never free-form updates.
/// </summary>
public sealed class WordState : AggregateRoot
{
    /// <summary>Stability (days) at which a word counts as Known / Mastered.</summary>
    public const double KnownStabilityDays = 7.0;
    public const double MasteredStabilityDays = 30.0;

    /// <summary>Lapses after which a word is flagged as a leech for special handling.</summary>
    public const int LeechThreshold = 8;

    private WordState()
    {
    }

    private WordState(Guid id, Guid learnerId, Guid vocabEntryId, WordSource source, string? contextSentence)
        : base(id)
    {
        LearnerId = learnerId;
        VocabEntryId = vocabEntryId;
        Source = source;
        ContextSentence = contextSentence;
        Status = WordStatus.New;
    }

    public Guid LearnerId { get; private set; }

    public Guid VocabEntryId { get; private set; }

    public WordStatus Status { get; private set; }

    public WordSource Source { get; private set; }

    public int LapseCount { get; private set; }

    public bool Leech { get; private set; }

    public string? PersonalNote { get; private set; }

    /// <summary>Sentence in which the learner met the word.</summary>
    public string? ContextSentence { get; private set; }

    public DateTimeOffset? LastReviewAt { get; private set; }

    public static WordState Meet(Guid learnerId, Guid vocabEntryId, WordSource source, string? contextSentence = null) =>
        new(Guid.CreateVersion7(), learnerId, vocabEntryId, source, contextSentence);

    /// <summary>Applies the FSRS outcome of a review to the status machine.</summary>
    public void ApplyReview(ReviewRating rating, FsrsState newState, DateTimeOffset now)
    {
        LastReviewAt = now;

        if (rating == ReviewRating.Again)
        {
            if (Status is WordStatus.Known or WordStatus.Mastered)
            {
                Status = WordStatus.Forgotten;
            }

            Status = Status == WordStatus.Forgotten ? WordStatus.Relearning : WordStatus.Learning;
            LapseCount++;
            if (LapseCount >= LeechThreshold)
            {
                Leech = true;
            }

            Raise(new WordLapsed(LearnerId, VocabEntryId, LapseCount, now));
            return;
        }

        var previous = Status;
        Status = newState.Stability switch
        {
            >= MasteredStabilityDays => WordStatus.Mastered,
            >= KnownStabilityDays => WordStatus.Known,
            _ => WordStatus.Learning,
        };

        if (Status == WordStatus.Mastered && previous != WordStatus.Mastered)
        {
            Raise(new WordMastered(LearnerId, VocabEntryId, now));
        }
    }

    public void SetPersonalNote(string? note) => PersonalNote = note;
}
