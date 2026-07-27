using System.Reflection;
using NetArchTest.Rules;

namespace Lif.Architecture.Tests;

/// <summary>
/// Enforces the dependency rules of `docs/14-project-structure.md` §2 (NFR-MNT-1):
/// SharedKernel ← Domain ← Application ← { Infrastructure, Persistence } ← Api.
/// </summary>
public class LayerDependencyTests
{
    private const string SharedKernel = "Lif.SharedKernel";
    private const string Domain = "Lif.Domain";
    private const string Application = "Lif.Application";
    private const string Infrastructure = "Lif.Infrastructure";
    private const string Persistence = "Lif.Persistence";
    private const string Api = "Lif.Api";

    private static readonly Assembly SharedKernelAssembly = typeof(Lif.SharedKernel.Results.Result).Assembly;
    private static readonly Assembly DomainAssembly = Lif.Domain.AssemblyReference.Assembly;
    private static readonly Assembly ApplicationAssembly = Lif.Application.AssemblyReference.Assembly;
    private static readonly Assembly InfrastructureAssembly = Lif.Infrastructure.AssemblyReference.Assembly;
    private static readonly Assembly PersistenceAssembly = typeof(Lif.Persistence.LifDbContext).Assembly;

    [Fact]
    public void SharedKernel_DependsOn_NoOtherLifAssembly()
    {
        var result = Types.InAssembly(SharedKernelAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Domain, Application, Infrastructure, Persistence, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureReport(result));
    }

    [Fact]
    public void Domain_DependsOnlyOn_SharedKernel()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Persistence, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureReport(result));
    }

    [Fact]
    public void Domain_HasNoDependencyOn_EfCore_Http_Or_AiSdks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "System.Net.Http",
                "Npgsql",
                "OpenAI",
                "Anthropic",
                "Azure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureReport(result));
    }

    [Fact]
    public void Application_DoesNotDependOn_OuterLayers()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Persistence, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureReport(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOn_Persistence_Or_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Persistence, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureReport(result));
    }

    [Fact]
    public void Persistence_DoesNotDependOn_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(PersistenceAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureReport(result));
    }

    private static string FailureReport(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
