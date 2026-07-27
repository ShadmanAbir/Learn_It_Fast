using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;
using Lif.SharedKernel.Results;

namespace Lif.Domain.Curriculum;

/// <summary>
/// Knowledge-graph node: an atomic teachable unit (doc 06 §3.1, doc 08).
/// Static curriculum content — never mutated by learner activity.
/// </summary>
public sealed class Concept : AggregateRoot
{
    private readonly List<ConceptDependency> _dependencies = [];
    private readonly List<ConceptExample> _examples = [];
    private readonly List<CommonMistake> _commonMistakes = [];
    private readonly List<Guid> _requiredVocabularyIds = [];

    private Concept()
    {
    }

    private Concept(
        Guid id,
        Guid versionId,
        string code,
        ConceptKind kind,
        CefrLevel level,
        Difficulty difficulty,
        string title,
        string learningObjective,
        string explanationMarkdown,
        int estimatedMinutes,
        double masteryThreshold,
        double importance,
        Guid? unitId)
        : base(id)
    {
        VersionId = versionId;
        Code = code;
        Kind = kind;
        Level = level;
        Difficulty = difficulty;
        Title = title;
        LearningObjective = learningObjective;
        ExplanationMarkdown = explanationMarkdown;
        EstimatedMinutes = estimatedMinutes;
        MasteryThreshold = masteryThreshold;
        Importance = importance;
        UnitId = unitId;
    }

    public Guid VersionId { get; private set; }

    /// <summary>Stable key (e.g. <c>gram.gender.articles.def</c>); learner state migrates across versions by code.</summary>
    public string Code { get; private set; } = string.Empty;

    public ConceptKind Kind { get; private set; }

    public CefrLevel Level { get; private set; }

    public Difficulty Difficulty { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string LearningObjective { get; private set; } = string.Empty;

    public string ExplanationMarkdown { get; private set; } = string.Empty;

    public int EstimatedMinutes { get; private set; }

    /// <summary>Mastery level at which the concept leaves the frontier; within [0.5, 0.95].</summary>
    public double MasteryThreshold { get; private set; }

    /// <summary>Weight in planner ranking and review priority.</summary>
    public double Importance { get; private set; }

    public Guid? UnitId { get; private set; }

    public IReadOnlyList<ConceptDependency> Dependencies => _dependencies.AsReadOnly();

    public IReadOnlyList<ConceptExample> Examples => _examples.AsReadOnly();

    public IReadOnlyList<CommonMistake> CommonMistakes => _commonMistakes.AsReadOnly();

    public IReadOnlyList<Guid> RequiredVocabularyIds => _requiredVocabularyIds.AsReadOnly();

    public static Result<Concept> Create(
        Guid versionId,
        string code,
        ConceptKind kind,
        CefrLevel level,
        Difficulty difficulty,
        string title,
        string learningObjective,
        string explanationMarkdown,
        int estimatedMinutes,
        double masteryThreshold = 0.80,
        double importance = 1.0,
        Guid? unitId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Concept>(CurriculumErrors.ConceptCodeRequired);
        }

        if (masteryThreshold is < 0.5 or > 0.95)
        {
            return Result.Failure<Concept>(CurriculumErrors.MasteryThresholdOutOfRange);
        }

        if (estimatedMinutes <= 0)
        {
            return Result.Failure<Concept>(CurriculumErrors.EstimatedMinutesInvalid);
        }

        return new Concept(
            Guid.CreateVersion7(), versionId, code.Trim(), kind, level, difficulty,
            title, learningObjective, explanationMarkdown, estimatedMinutes,
            masteryThreshold, importance, unitId);
    }

    public Result AddDependency(Guid prerequisiteId, DependencyType type, double minMastery = 0.80)
    {
        if (prerequisiteId == Id)
        {
            return Result.Failure(CurriculumErrors.SelfDependency);
        }

        if (_dependencies.Any(d => d.PrerequisiteId == prerequisiteId))
        {
            return Result.Failure(CurriculumErrors.DuplicateDependency);
        }

        _dependencies.Add(new ConceptDependency(prerequisiteId, type, minMastery));
        return Result.Success();
    }

    public void AddExample(string german, string english, string? note = null) =>
        _examples.Add(new ConceptExample(german, english, note, _examples.Count));

    public void AddCommonMistake(string wrong, string right, string explanation, string errorCode) =>
        _commonMistakes.Add(new CommonMistake(wrong, right, explanation, errorCode));

    public void AddRequiredVocabulary(Guid vocabEntryId)
    {
        if (!_requiredVocabularyIds.Contains(vocabEntryId))
        {
            _requiredVocabularyIds.Add(vocabEntryId);
        }
    }

    /// <summary>Hard prerequisites only — the edges that gate frontier entry.</summary>
    public IEnumerable<ConceptDependency> Requires =>
        _dependencies.Where(d => d.Type == DependencyType.Requires);
}
