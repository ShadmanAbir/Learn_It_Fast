using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Learner;

/// <summary>Learner profile: identity, goals, pacing preferences (doc 06 §3.2).</summary>
public sealed class LearnerProfile : AggregateRoot
{
    private LearnerProfile()
    {
    }

    private LearnerProfile(
        Guid id,
        string displayName,
        LanguageCode uiLanguage,
        CefrLevel targetLevel,
        int minutesPerDay,
        Guid curriculumVersionId,
        string? motivationType)
        : base(id)
    {
        DisplayName = displayName;
        UiLanguage = uiLanguage;
        TargetLevel = targetLevel;
        MinutesPerDay = minutesPerDay;
        CurriculumVersionId = curriculumVersionId;
        MotivationType = motivationType;
        CurrentLevel = CefrLevel.A1_1;
    }

    public string DisplayName { get; private set; } = string.Empty;

    public LanguageCode UiLanguage { get; private set; }

    public CefrLevel TargetLevel { get; private set; }

    /// <summary>Overall working level; derived from skill estimates in Phase 2+ (never set by an LLM).</summary>
    public CefrLevel CurrentLevel { get; private set; }

    public int MinutesPerDay { get; private set; }

    public Guid CurriculumVersionId { get; private set; }

    /// <summary>career | exam | relocation | hobby</summary>
    public string? MotivationType { get; private set; }

    /// <summary>Beginner fast path (Phase 1): skip placement, start at A1.1 with zero mastery.</summary>
    public static LearnerProfile CreateBeginner(
        string displayName,
        CefrLevel targetLevel,
        int minutesPerDay,
        Guid curriculumVersionId,
        string? motivationType = null)
    {
        var minutes = Math.Clamp(minutesPerDay, 5, 240);
        return new LearnerProfile(
            Guid.CreateVersion7(), displayName.Trim(), LanguageCode.English,
            targetLevel, minutes, curriculumVersionId, motivationType);
    }

    public void UpdateGoals(CefrLevel targetLevel, int minutesPerDay)
    {
        TargetLevel = targetLevel;
        MinutesPerDay = Math.Clamp(minutesPerDay, 5, 240);
    }
}
