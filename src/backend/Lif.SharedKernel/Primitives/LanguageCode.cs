namespace Lif.SharedKernel.Primitives;

/// <summary>ISO 639-1 language code. v1 ships German ("de") with UI languages en/de.</summary>
public readonly record struct LanguageCode
{
    private LanguageCode(string value) => Value = value;

    public string Value { get; }

    public static LanguageCode German => new("de");

    public static LanguageCode English => new("en");

    public static LanguageCode From(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length == 2
            ? new LanguageCode(value.Trim().ToLowerInvariant())
            : throw new ArgumentException("Language code must be a two-letter ISO 639-1 code.", nameof(value));

    public override string ToString() => Value;
}
