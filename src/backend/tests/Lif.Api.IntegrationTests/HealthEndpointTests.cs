using Microsoft.AspNetCore.Mvc.Testing;

namespace Lif.Api.IntegrationTests;

/// <summary>Boots the real host (no DB configured → ready check trivially passes) and hits the health endpoints.</summary>
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ReturnHealthy(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.IsSuccessStatusCode.Should().BeTrue();
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task Root_ServesSpaIndex()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/", UriKind.Relative));

        response.IsSuccessStatusCode.Should().BeTrue();
        (await response.Content.ReadAsStringAsync()).Should().Contain("LIF");
    }
}
