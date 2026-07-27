using Lif.Domain.Learner;

namespace Lif.Domain.Tests.Learner;

public class MasteryUpdaterTests
{
    [Fact]
    public void CorrectAnswer_IncreasesMastery()
    {
        var updated = MasteryUpdater.Update(mastery: 0.3, outcome: 1.0);

        updated.Should().BeGreaterThan(0.3);
    }

    [Fact]
    public void WrongAnswer_LowersPosteriorBelowCorrectAnswer()
    {
        var afterCorrect = MasteryUpdater.Update(mastery: 0.5, outcome: 1.0);
        var afterWrong = MasteryUpdater.Update(mastery: 0.5, outcome: 0.0);

        afterWrong.Should().BeLessThan(afterCorrect);
    }

    [Fact]
    public void PartialCredit_LandsBetweenWrongAndCorrect()
    {
        var afterCorrect = MasteryUpdater.Update(mastery: 0.5, outcome: 1.0);
        var afterPartial = MasteryUpdater.Update(mastery: 0.5, outcome: 0.5);
        var afterWrong = MasteryUpdater.Update(mastery: 0.5, outcome: 0.0);

        afterPartial.Should().BeGreaterThan(afterWrong).And.BeLessThan(afterCorrect);
    }

    [Fact]
    public void Hints_ReduceCreditForACorrectAnswer()
    {
        var noHints = MasteryUpdater.Update(mastery: 0.5, outcome: 1.0, hintsUsed: 0);
        var twoHints = MasteryUpdater.Update(mastery: 0.5, outcome: 1.0, hintsUsed: 2);

        twoHints.Should().BeLessThan(noHints);
    }

    [Fact]
    public void FourHints_ZeroCredit_MatchesWrongAnswer()
    {
        var fourHints = MasteryUpdater.Update(mastery: 0.5, outcome: 1.0, hintsUsed: 4);
        var wrong = MasteryUpdater.Update(mastery: 0.5, outcome: 0.0);

        fourHints.Should().BeApproximately(wrong, 1e-12);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 0.0)]
    public void Result_StaysWithinUnitInterval(double mastery, double outcome)
    {
        var updated = MasteryUpdater.Update(mastery, outcome);

        updated.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void HigherGuess_MakesCorrectAnswerLessInformative()
    {
        var lowGuess = MasteryUpdater.Update(mastery: 0.5, outcome: 1.0, hintsUsed: 0, guess: 0.1);
        var highGuess = MasteryUpdater.Update(mastery: 0.5, outcome: 1.0, hintsUsed: 0, guess: 0.5);

        highGuess.Should().BeLessThan(lowGuess);
    }

    [Theory]
    [InlineData(2, 0.5)]
    [InlineData(4, 0.25)]
    [InlineData(5, 0.2)]
    public void GuessForMultipleChoice_IsInverseOptionCount(int optionCount, double expected)
    {
        MasteryUpdater.GuessForMultipleChoice(optionCount).Should().BeApproximately(expected, 1e-12);
    }

    [Fact]
    public void GuessForMultipleChoice_FallsBackToDefaultForDegenerateCounts()
    {
        MasteryUpdater.GuessForMultipleChoice(1).Should().BeApproximately(0.2, 1e-12);
        MasteryUpdater.GuessForMultipleChoice(0).Should().BeApproximately(0.2, 1e-12);
    }
}
