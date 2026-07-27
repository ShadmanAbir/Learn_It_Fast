namespace Lif.Domain.Learner;

/// <summary>
/// Pure BKT-style Bayesian mastery update (doc 08 §6). Guess/slip priors are concept-level
/// (defaults g=0.2, s=0.1); hint usage scales outcome credit ×(1 − 0.25·hints).
/// </summary>
public static class MasteryUpdater
{
    public const double DefaultGuess = 0.20;
    public const double DefaultSlip = 0.10;

    /// <summary>Probability of transitioning to "known" after one practice opportunity.</summary>
    public const double DefaultLearnRate = 0.10;

    /// <param name="mastery">Prior P(known) ∈ [0,1].</param>
    /// <param name="outcome">Observed outcome ∈ [0,1] (partial credit).</param>
    /// <param name="hintsUsed">Hints consumed; each hint removes 25 % of credit.</param>
    /// <param name="guess">P(correct | not known).</param>
    /// <param name="slip">P(wrong | known).</param>
    /// <param name="learnRate">P(transit) applied after the evidence update.</param>
    /// <returns>Posterior P(known), clamped to [0,1].</returns>
    public static double Update(
        double mastery,
        double outcome,
        int hintsUsed = 0,
        double guess = DefaultGuess,
        double slip = DefaultSlip,
        double learnRate = DefaultLearnRate)
    {
        var prior = Math.Clamp(mastery, 0.0, 1.0);
        var credit = Math.Clamp(outcome, 0.0, 1.0) * Math.Max(0.0, 1.0 - (0.25 * hintsUsed));

        // Evidence: blend the correct/incorrect posteriors by partial credit.
        var pCorrect = (prior * (1 - slip)) + ((1 - prior) * guess);
        var posteriorIfCorrect = pCorrect > 0 ? prior * (1 - slip) / pCorrect : prior;

        var pWrong = (prior * slip) + ((1 - prior) * (1 - guess));
        var posteriorIfWrong = pWrong > 0 ? prior * slip / pWrong : prior;

        var evidencePosterior = (credit * posteriorIfCorrect) + ((1 - credit) * posteriorIfWrong);

        // Learning transit: every practice opportunity teaches a little.
        var posterior = evidencePosterior + ((1 - evidencePosterior) * learnRate);
        return Math.Clamp(posterior, 0.0, 1.0);
    }

    /// <summary>Option-count-aware guess for multiple-choice items (doc 08 §6).</summary>
    public static double GuessForMultipleChoice(int optionCount) =>
        optionCount > 1 ? 1.0 / optionCount : DefaultGuess;
}
