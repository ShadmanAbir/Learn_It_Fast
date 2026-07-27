using Lif.Domain.Curriculum;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Tests.Curriculum;

public class FrontierCalculatorTests
{
    private static readonly Guid VersionId = Guid.CreateVersion7();

    private static Concept MakeConcept(
        string code,
        CefrLevel? level = null,
        double masteryThreshold = 0.8,
        double importance = 1.0,
        Guid? unitId = null)
    {
        var result = Concept.Create(
            VersionId, code, ConceptKind.Grammar, level ?? CefrLevel.A1_1, Difficulty.From(2),
            title: code, learningObjective: "objective", explanationMarkdown: "explanation",
            estimatedMinutes: 5, masteryThreshold, importance, unitId);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private static IReadOnlyList<FrontierEntry> Compute(
        IReadOnlyList<Concept> concepts,
        IReadOnlyDictionary<Guid, double>? mastery = null,
        IReadOnlyDictionary<Guid, int>? unitOrder = null) =>
        FrontierCalculator.Compute(
            concepts,
            mastery ?? new Dictionary<Guid, double>(),
            CefrLevel.A1_1,
            unitOrder ?? new Dictionary<Guid, int>());

    [Fact]
    public void ConceptWithoutPrerequisites_IsOnTheFrontier()
    {
        var concept = MakeConcept("gram.a");

        var frontier = Compute([concept]);

        frontier.Should().ContainSingle(e => e.ConceptId == concept.Id);
    }

    [Fact]
    public void MasteredConcept_LeavesTheFrontier()
    {
        var concept = MakeConcept("gram.a", masteryThreshold: 0.8);
        var mastery = new Dictionary<Guid, double> { [concept.Id] = 0.85 };

        Compute([concept], mastery).Should().BeEmpty();
    }

    [Fact]
    public void UnmetHardPrerequisite_BlocksTheConcept()
    {
        var prereq = MakeConcept("gram.a");
        var dependent = MakeConcept("gram.b");
        dependent.AddDependency(prereq.Id, DependencyType.Requires, minMastery: 0.8).IsSuccess.Should().BeTrue();

        var frontier = Compute([prereq, dependent]);

        frontier.Should().ContainSingle(e => e.ConceptId == prereq.Id);
    }

    [Fact]
    public void MetPrerequisite_UnlocksTheDependent()
    {
        var prereq = MakeConcept("gram.a");
        var dependent = MakeConcept("gram.b");
        dependent.AddDependency(prereq.Id, DependencyType.Requires, minMastery: 0.8).IsSuccess.Should().BeTrue();
        var mastery = new Dictionary<Guid, double> { [prereq.Id] = 0.85 };

        var frontier = Compute([prereq, dependent], mastery);

        frontier.Should().ContainSingle(e => e.ConceptId == dependent.Id);
    }

    [Fact]
    public void CloserCefrLevel_RanksFirst()
    {
        var far = MakeConcept("gram.far", CefrLevel.A2_1);
        var near = MakeConcept("gram.near", CefrLevel.A1_1);

        var frontier = Compute([far, near]);

        frontier.Select(e => e.Code).Should().ContainInOrder("gram.near", "gram.far");
    }

    [Fact]
    public void HigherImportance_BreaksTiesAtSameLevel()
    {
        var minor = MakeConcept("gram.minor", importance: 0.3);
        var core = MakeConcept("gram.core", importance: 0.9);

        var frontier = Compute([minor, core]);

        frontier.Select(e => e.Code).Should().ContainInOrder("gram.core", "gram.minor");
    }

    [Fact]
    public void UnlockCount_ReflectsHardDependents()
    {
        var hub = MakeConcept("gram.hub");
        var leafA = MakeConcept("gram.leaf-a");
        var leafB = MakeConcept("gram.leaf-b");
        leafA.AddDependency(hub.Id, DependencyType.Requires).IsSuccess.Should().BeTrue();
        leafB.AddDependency(hub.Id, DependencyType.Requires).IsSuccess.Should().BeTrue();

        var frontier = Compute([hub, leafA, leafB]);

        frontier.Single(e => e.ConceptId == hub.Id).UnlockCount.Should().Be(2);
    }

    [Fact]
    public void EqualSignals_FallBackToOrdinalCodeOrder_Deterministically()
    {
        var b = MakeConcept("gram.b");
        var a = MakeConcept("gram.a");

        var first = Compute([b, a]);
        var second = Compute([a, b]);

        first.Select(e => e.Code).Should().ContainInOrder("gram.a", "gram.b");
        first.Select(e => e.ConceptId).Should().Equal(second.Select(e => e.ConceptId));
    }
}
