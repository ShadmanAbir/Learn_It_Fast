using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Curriculum;

/// <summary>Kind of exam a blueprint defines.</summary>
public enum BlueprintKind
{
    Checkpoint = 1,
    LevelUp = 2,
    PlacementBank = 3,
}

/// <summary>One skill section of an exam blueprint.</summary>
public sealed record BlueprintSection(Skill Skill, int ItemCount, double PassPercentage);

/// <summary>Checkpoint / level-up exam definition (doc 06 §3.1; doc 09 §4).</summary>
public sealed class AssessmentBlueprint : AggregateRoot
{
    private readonly List<BlueprintSection> _sections = [];

    private AssessmentBlueprint()
    {
    }

    private AssessmentBlueprint(Guid id, Guid versionId, BlueprintKind kind, CefrLevel level, Guid? unitId)
        : base(id)
    {
        VersionId = versionId;
        Kind = kind;
        Level = level;
        UnitId = unitId;
    }

    public Guid VersionId { get; private set; }

    public BlueprintKind Kind { get; private set; }

    public CefrLevel Level { get; private set; }

    /// <summary>Set for checkpoint blueprints; null for level-up exams.</summary>
    public Guid? UnitId { get; private set; }

    public IReadOnlyList<BlueprintSection> Sections => _sections.AsReadOnly();

    public static AssessmentBlueprint Create(Guid versionId, BlueprintKind kind, CefrLevel level, Guid? unitId = null) =>
        new(Guid.CreateVersion7(), versionId, kind, level, unitId);

    public void AddSection(Skill skill, int itemCount, double passPercentage) =>
        _sections.Add(new BlueprintSection(skill, itemCount, passPercentage));
}
