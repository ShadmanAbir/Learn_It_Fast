using NetArchTest.Rules;

namespace Lif.Architecture.Tests;

/// <summary>
/// Rule 4 of `docs/14-project-structure.md` §2: planner/scheduler code decides *what* to teach
/// before any content is generated — it must never reference AI generation ports.
/// The rule is in place from day 1 and bites as soon as the namespaces gain types (Phase 1).
/// </summary>
public class PlannerIsolationTests
{
    private static readonly string[] GenerationPortNamespaces =
    [
        "Lif.Application.Ai",
    ];

    [Fact]
    public void DomainPlanning_DoesNotReference_AiGenerationPorts()
    {
        var result = Types.InAssembly(Lif.Domain.AssemblyReference.Assembly)
            .That().ResideInNamespaceStartingWith("Lif.Domain")
            .ShouldNot().HaveDependencyOnAny(GenerationPortNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Report(result));
    }

    [Fact]
    public void ApplicationLearningPlanner_DoesNotReference_AiGenerationPorts()
    {
        var types = Types.InAssembly(Lif.Application.AssemblyReference.Assembly)
            .That().ResideInNamespaceStartingWith("Lif.Application.Learning")
            .GetTypes();

        // Namespace is empty until Phase 1; the rule activates automatically once planner code lands.
        if (!types.Any())
        {
            return;
        }

        var result = Types.InAssembly(Lif.Application.AssemblyReference.Assembly)
            .That().ResideInNamespaceStartingWith("Lif.Application.Learning")
            .ShouldNot().HaveDependencyOnAny(GenerationPortNamespaces)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Report(result));
    }

    private static string Report(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
