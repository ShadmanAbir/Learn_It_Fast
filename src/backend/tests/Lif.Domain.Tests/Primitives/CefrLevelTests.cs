using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Tests.Primitives;

public class CefrLevelTests
{
    [Theory]
    [InlineData("A1.1", 0)]
    [InlineData("a1.2", 1)]
    [InlineData("B1", 4)]
    [InlineData("B2.2", 7)]
    [InlineData("C1.2", 9)]
    public void TryParse_AcceptsValidLevels(string text, int expectedRank)
    {
        CefrLevel.TryParse(text, out var level).Should().BeTrue();
        level.Rank.Should().Be(expectedRank);
    }

    [Theory]
    [InlineData("C2")]
    [InlineData("A0")]
    [InlineData("B1.3")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_RejectsInvalidLevels(string? text)
    {
        CefrLevel.TryParse(text, out _).Should().BeFalse();
    }

    [Fact]
    public void Levels_AreTotallyOrdered()
    {
        CefrLevel.A1_1.Should().BeLessThan(CefrLevel.A1_2);
        CefrLevel.A2_2.Should().BeLessThan(CefrLevel.B1_1);
        CefrLevel.C1_2.Should().BeGreaterThan(CefrLevel.B2_2);
    }

    [Fact]
    public void ToString_RoundTripsThroughTryParse()
    {
        for (var rank = 0; rank <= 9; rank++)
        {
            var level = CefrLevel.FromRank(rank);
            CefrLevel.TryParse(level.ToString(), out var parsed).Should().BeTrue();
            parsed.Should().Be(level);
        }
    }
}
