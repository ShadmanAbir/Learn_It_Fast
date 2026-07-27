namespace Lif.SharedKernel.Primitives;

/// <summary>
/// CEFR level with sub-level granularity (A1.1 … C1.2), totally ordered.
/// The application derives these from skill estimates — never from LLM output (ADR-001).
/// </summary>
public readonly record struct CefrLevel : IComparable<CefrLevel>
{
    private static readonly string[] Majors = ["A1", "A2", "B1", "B2", "C1"];

    private CefrLevel(int rank) => Rank = rank;

    /// <summary>Zero-based total order: A1.1 = 0, A1.2 = 1, … C1.2 = 9.</summary>
    public int Rank { get; }

    public string Major => Majors[Rank / 2];

    public int SubLevel => (Rank % 2) + 1;

    public static CefrLevel A1_1 => new(0);
    public static CefrLevel A1_2 => new(1);
    public static CefrLevel A2_1 => new(2);
    public static CefrLevel A2_2 => new(3);
    public static CefrLevel B1_1 => new(4);
    public static CefrLevel B1_2 => new(5);
    public static CefrLevel B2_1 => new(6);
    public static CefrLevel B2_2 => new(7);
    public static CefrLevel C1_1 => new(8);
    public static CefrLevel C1_2 => new(9);

    public static CefrLevel FromRank(int rank) =>
        rank is >= 0 and <= 9
            ? new CefrLevel(rank)
            : throw new ArgumentOutOfRangeException(nameof(rank), rank, "CEFR rank must be within 0..9 (A1.1..C1.2).");

    public static bool TryParse(string? text, out CefrLevel level)
    {
        level = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Trim().ToUpperInvariant().Split('.');
        var majorIndex = Array.IndexOf(Majors, parts[0]);
        if (majorIndex < 0)
        {
            return false;
        }

        var sub = 1;
        if (parts.Length == 2 && (!int.TryParse(parts[1], out sub) || sub is < 1 or > 2))
        {
            return false;
        }

        level = new CefrLevel((majorIndex * 2) + (sub - 1));
        return true;
    }

    public int CompareTo(CefrLevel other) => Rank.CompareTo(other.Rank);

    public static bool operator <(CefrLevel left, CefrLevel right) => left.Rank < right.Rank;
    public static bool operator >(CefrLevel left, CefrLevel right) => left.Rank > right.Rank;
    public static bool operator <=(CefrLevel left, CefrLevel right) => left.Rank <= right.Rank;
    public static bool operator >=(CefrLevel left, CefrLevel right) => left.Rank >= right.Rank;

    public override string ToString() => $"{Major}.{SubLevel}";
}
