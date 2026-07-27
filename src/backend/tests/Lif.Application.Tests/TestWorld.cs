using Lif.Application.Attempts;
using Lif.Application.Learners;
using Lif.Application.Reviews;
using Lif.Application.Sessions;
using Lif.Application.Tests.Fakes;
using Lif.Application.Today;
using Lif.Domain.Curriculum;
using Lif.Domain.Learning;
using Lif.SharedKernel.Primitives;

namespace Lif.Application.Tests;

/// <summary>
/// Wires all handlers against in-memory fakes with a tiny A1 curriculum:
/// greet (no deps, 2 MC), wfragen (requires greet, 1 MC), numbers (no deps, 1 cloze).
/// </summary>
public sealed class TestWorld
{
    public static readonly DateTimeOffset Start = new(2026, 7, 26, 8, 0, 0, TimeSpan.Zero);

    public TestWorld()
    {
        Version = CurriculumVersion.CreateDraft(LanguageCode.German, "0.1.0-test");
        Version.Publish();
        Curriculum.ActiveVersion = Version;

        var unit = Unit.Create(Version.Id, CefrLevel.A1_1, 0, "Erste Kontakte");
        Curriculum.UnitOrder[unit.Id] = unit.OrderIndex;

        Greet = NewConcept("gram.greet", "Greetings", importance: 1.0, unitId: unit.Id);
        WFragen = NewConcept("gram.wfragen", "W-Fragen", importance: 0.9, unitId: unit.Id);
        Numbers = NewConcept("voc.numbers", "Numbers", importance: 0.5, unitId: unit.Id);
        WFragen.AddDependency(Greet.Id, DependencyType.Requires, 0.8);
        Curriculum.Concepts.AddRange([Greet, WFragen, Numbers]);

        AddExercise("ex.greet.mc-1", new ExerciseSpec(
            ExerciseType.MultipleChoice, "Wie sagt man 'good morning'?",
            ["Guten Morgen", "Gute Nacht", "Hallo"], "Guten Morgen", [Greet.Id],
            Difficulty.From(1), "‚Guten Morgen' bis ca. 11 Uhr.", "GREET_TIME"));
        AddExercise("ex.greet.mc-2", new ExerciseSpec(
            ExerciseType.MultipleChoice, "Wie sagt man 'good evening'?",
            ["Guten Abend", "Guten Tag"], "Guten Abend", [Greet.Id],
            Difficulty.From(1)));
        AddExercise("ex.wfragen.mc-1", new ExerciseSpec(
            ExerciseType.MultipleChoice, "___ heißt du?",
            ["Wie", "Wo", "Was"], "Wie", [WFragen.Id],
            Difficulty.From(2), null, "WO_V2"));
        AddExercise("ex.numbers.cloze-1", new ExerciseSpec(
            ExerciseType.Cloze, "zwei plus zwei ist ___",
            [], "vier", [Numbers.Id],
            Difficulty.From(1)));
    }

    public FixedClock Clock { get; } = new(Start);

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public FakeCurriculumReader Curriculum { get; } = new();

    public FakeLearnerRepository Learners { get; } = new();

    public FakeConceptMasteryRepository Masteries { get; } = new();

    public FakeWordStateRepository WordStates { get; } = new();

    public FakeMistakeRepository Mistakes { get; } = new();

    public FakeStudySessionRepository Sessions { get; } = new();

    public FakeReviewItemRepository Reviews { get; } = new();

    public CurriculumVersion Version { get; }

    public Concept Greet { get; }

    public Concept WFragen { get; }

    public Concept Numbers { get; }

    public OnboardLearnerHandler OnboardLearner => new(Curriculum, Learners, UnitOfWork);

    public GetLearnerHandler GetLearner => new(Learners);

    public UpdateGoalsHandler UpdateGoals => new(Learners, UnitOfWork);

    public PlanSessionHandler PlanSession =>
        new(Learners, Curriculum, Masteries, Reviews, Sessions, UnitOfWork, Clock);

    public GetCurrentSessionHandler GetCurrentSession => new(Sessions);

    public SessionLifecycleHandler SessionLifecycle => new(Sessions, UnitOfWork, Clock);

    public RecordAttemptHandler RecordAttempt =>
        new(Sessions, Masteries, Mistakes, Reviews, UnitOfWork, Clock);

    public GetReviewQueueHandler GetReviewQueue => new(Reviews, UnitOfWork, Clock);

    public AnswerReviewHandler AnswerReview => new(Reviews, WordStates, UnitOfWork, Clock);

    public GetTodayHandler GetToday => new(Learners, Sessions, Reviews, Clock);

    /// <summary>Onboards a fresh beginner and returns the learner id.</summary>
    public async Task<Guid> OnboardAsync(int minutesPerDay = 15)
    {
        var result = await OnboardLearner.HandleAsync(
            new OnboardLearnerCommand("Alex", "B1", minutesPerDay));
        result.IsSuccess.Should().BeTrue();
        return result.Value.LearnerId;
    }

    private Concept NewConcept(string code, string title, double importance, Guid unitId)
    {
        var concept = Concept.Create(
            Version.Id, code, ConceptKind.Grammar, CefrLevel.A1_1, Difficulty.From(1),
            title, $"Can use {title}.", $"# {title}", estimatedMinutes: 5,
            importance: importance, unitId: unitId);
        concept.IsSuccess.Should().BeTrue();
        return concept.Value;
    }

    private void AddExercise(string code, ExerciseSpec spec)
    {
        var exercise = CuratedExercise.Create(Version.Id, code, spec);
        exercise.IsSuccess.Should().BeTrue();
        Curriculum.Exercises.Add(exercise.Value);
    }
}
