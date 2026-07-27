using Lif.SharedKernel.Domain;

namespace Lif.Domain.Learner;

/// <summary>
/// Per-learner mastery state for one concept (doc 06 §3.2).
/// Invariant: mastery only changes through recorded observations.
/// </summary>
public sealed class ConceptMastery : AggregateRoot
{
    /// <summary>Wrong-answer streak that triggers remediation (FR-PLN-4, doc 08 §6).</summary>
    public const int RemediationStreak = 3;

    private ConceptMastery()
    {
    }

    private ConceptMastery(Guid id, Guid learnerId, Guid conceptId, double mastery, bool provisional)
        : base(id)
    {
        LearnerId = learnerId;
        ConceptId = conceptId;
        Mastery = mastery;
        Provisional = provisional;
    }

    public Guid LearnerId { get; private set; }

    public Guid ConceptId { get; private set; }

    /// <summary>P(learner knows concept) ∈ [0,1].</summary>
    public double Mastery { get; private set; }

    /// <summary>Estimate confidence ∈ [0,1); saturates with observation count.</summary>
    public double Confidence { get; private set; }

    public int Attempts { get; private set; }

    public int Correct { get; private set; }

    public int StreakWrong { get; private set; }

    /// <summary>Placement-seeded value not yet confirmed by direct observations.</summary>
    public bool Provisional { get; private set; }

    public DateTimeOffset? LastPracticedAt { get; private set; }

    public static ConceptMastery Start(Guid learnerId, Guid conceptId) =>
        new(Guid.CreateVersion7(), learnerId, conceptId, mastery: 0, provisional: false);

    public static ConceptMastery SeedFromPlacement(Guid learnerId, Guid conceptId, double mastery) =>
        new(Guid.CreateVersion7(), learnerId, conceptId, Math.Clamp(mastery, 0, 1), provisional: true);

    /// <summary>Applies one scored observation via the BKT updater. The only mutation path.</summary>
    public void ApplyObservation(double outcome, int hintsUsed, double guess, double slip, DateTimeOffset now)
    {
        var previous = Mastery;
        Mastery = MasteryUpdater.Update(Mastery, outcome, hintsUsed, guess, slip);

        Attempts++;
        var counted = outcome >= 0.5;
        if (counted)
        {
            Correct++;
            StreakWrong = 0;
        }
        else
        {
            StreakWrong++;
        }

        Confidence = Attempts / (Attempts + 5.0);
        Provisional = false;
        LastPracticedAt = now;

        Raise(new ConceptMasteryChanged(LearnerId, ConceptId, previous, Mastery, now));
    }

    /// <summary>Remediation cut (doc 08 §6): a failed prerequisite re-check caps mastery at 0.6.</summary>
    public void CutForRemediation(DateTimeOffset now)
    {
        var previous = Mastery;
        Mastery = Math.Min(Mastery, 0.6);
        StreakWrong = 0;
        if (Math.Abs(Mastery - previous) > double.Epsilon)
        {
            Raise(new ConceptMasteryChanged(LearnerId, ConceptId, previous, Mastery, now));
        }
    }

    public bool NeedsRemediation => StreakWrong >= RemediationStreak;
}
