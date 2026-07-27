namespace Lif.SharedKernel.Primitives;

/// <summary>Item/concept difficulty on the authoring scale 1–10, with band arithmetic used by the DifficultyGovernor.</summary>
public readonly record struct Difficulty : IComparable<Difficulty>
{
    public const int Min = 1;
    public const int Max = 10;

    private Difficulty(int value) => Value = value;

    public int Value { get; }

    public static Difficulty From(int value) =>
        value is >= Min and <= Max
            ? new Difficulty(value)
            : throw new ArgumentOutOfRangeException(nameof(value), value, $"Difficulty must be within {Min}..{Max}.");

    /// <summary>Moves by at most one band; the governor never jumps more than one band per session.</summary>
    public Difficulty StepUp() => new(Math.Min(Value + 1, Max));

    public Difficulty StepDown() => new(Math.Max(Value - 1, Min));

    public int CompareTo(Difficulty other) => Value.CompareTo(other.Value);

    public static bool operator <(Difficulty left, Difficulty right) => left.Value < right.Value;
    public static bool operator >(Difficulty left, Difficulty right) => left.Value > right.Value;
    public static bool operator <=(Difficulty left, Difficulty right) => left.Value <= right.Value;
    public static bool operator >=(Difficulty left, Difficulty right) => left.Value >= right.Value;

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
