using Lif.Domain.Learning;

namespace Lif.Simulation.Tests;

/// <summary>
/// Behavioural parameters of a virtual learner (doc 17 §2): answer accuracy, session skipping
/// and a fixed seed so every journey is reproducible.
/// </summary>
public sealed record StudentProfile(
    string Name,
    double Accuracy,
    double SkipProbability,
    int Seed,
    int MinutesPerDay = 15)
{
    public static StudentProfile Diligent(int seed = 1) => new("diligent", 0.95, 0.0, seed);

    public static StudentProfile Struggling(int seed = 1) => new("struggling", 0.40, 0.25, seed);
}

/// <summary>
/// Turns a profile into concrete behaviour: answers exercises from the curated answer key
/// (correct with probability <see cref="StudentProfile.Accuracy"/>) and rates reviews.
/// </summary>
public sealed class SimulatedStudent(StudentProfile profile)
{
    private readonly Random _random = new(profile.Seed);

    public StudentProfile Profile { get; } = profile;

    /// <summary>True when today's study session is skipped entirely.</summary>
    public bool SkipsToday() => _random.NextDouble() < Profile.SkipProbability;

    /// <summary>The learner's response for an exercise, given the curated spec.</summary>
    public string AnswerFor(ExerciseSpec spec)
    {
        if (_random.NextDouble() < Profile.Accuracy)
        {
            return spec.AnswerKey;
        }

        // A plausible wrong answer: another option for MC, garbage for free-form types.
        var distractor = spec.Options.FirstOrDefault(o => o != spec.AnswerKey);
        return distractor ?? "falsch";
    }

    /// <summary>FSRS self-rating for a review card: 3 = Good when recalled, 1 = Again when not.</summary>
    public int RateReview() => _random.NextDouble() < Profile.Accuracy ? 3 : 1;
}
