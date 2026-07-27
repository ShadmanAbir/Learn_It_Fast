using Lif.Domain.Curriculum;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Tests.Curriculum;

public class KnowledgeGraphValidatorTests
{
    private static Concept MakeConcept(string code)
    {
        var result = Concept.Create(
            Guid.CreateVersion7(), code, ConceptKind.Grammar, CefrLevel.A1_1, Difficulty.From(2),
            title: code, learningObjective: "objective", explanationMarkdown: "explanation", estimatedMinutes: 5);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public void EmptyGraph_IsValid()
    {
        KnowledgeGraphValidator.EnsureAcyclic([]).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LinearChain_IsValid()
    {
        var a = MakeConcept("gram.a");
        var b = MakeConcept("gram.b");
        var c = MakeConcept("gram.c");
        b.AddDependency(a.Id, DependencyType.Requires);
        c.AddDependency(b.Id, DependencyType.Requires);

        KnowledgeGraphValidator.EnsureAcyclic([a, b, c]).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void DirectCycle_IsRejected()
    {
        var a = MakeConcept("gram.a");
        var b = MakeConcept("gram.b");
        a.AddDependency(b.Id, DependencyType.Requires);
        b.AddDependency(a.Id, DependencyType.Requires);

        var result = KnowledgeGraphValidator.EnsureAcyclic([a, b]);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CurriculumErrors.CycleDetected);
    }

    [Fact]
    public void LongerCycle_IsRejected()
    {
        var a = MakeConcept("gram.a");
        var b = MakeConcept("gram.b");
        var c = MakeConcept("gram.c");
        b.AddDependency(a.Id, DependencyType.Requires);
        c.AddDependency(b.Id, DependencyType.Requires);
        a.AddDependency(c.Id, DependencyType.Requires);

        KnowledgeGraphValidator.EnsureAcyclic([a, b, c]).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CycleThroughSupportsEdges_IsAlsoRejected()
    {
        var a = MakeConcept("gram.a");
        var b = MakeConcept("gram.b");
        a.AddDependency(b.Id, DependencyType.Supports);
        b.AddDependency(a.Id, DependencyType.Supports);

        KnowledgeGraphValidator.EnsureAcyclic([a, b]).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DanglingPrerequisiteReference_IsTolerated()
    {
        var a = MakeConcept("gram.a");
        a.AddDependency(Guid.CreateVersion7(), DependencyType.Requires); // not in the concept set

        KnowledgeGraphValidator.EnsureAcyclic([a]).IsSuccess.Should().BeTrue();
    }
}
