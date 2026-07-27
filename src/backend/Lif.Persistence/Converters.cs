using System.Text;
using Lif.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lif.Persistence;

/// <summary>
/// Enum ↔ snake_case text (doc 07 stores enums as lowercase text:
/// <c>level_up</c>, <c>ai_generated</c>, <c>multiple_choice</c> …).
/// </summary>
public static class EnumText
{
    public static string Write<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }

    public static TEnum Read<TEnum>(string text)
        where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(text.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true);
}

/// <summary>Value converters for SharedKernel primitives and enums (doc 07 column conventions).</summary>
public static class Converters
{
    /// <summary>CEFR level ↔ text (<c>A1.1</c> … <c>C1.2</c>).</summary>
    public static readonly ValueConverter<CefrLevel, string> CefrLevelToText =
        new(level => level.ToString(), text => ParseCefr(text));

    /// <summary>Authoring difficulty ↔ smallint (1–10).</summary>
    public static readonly ValueConverter<Difficulty, short> DifficultyToSmallint =
        new(difficulty => (short)difficulty.Value, value => Difficulty.From(value));

    /// <summary>ISO 639-1 code ↔ text.</summary>
    public static readonly ValueConverter<LanguageCode, string> LanguageCodeToText =
        new(code => code.Value, text => LanguageCode.From(text));

    public static ValueConverter<TEnum, string> EnumToSnakeCaseText<TEnum>()
        where TEnum : struct, Enum =>
        new(value => EnumText.Write(value), text => EnumText.Read<TEnum>(text));

    private static CefrLevel ParseCefr(string text) =>
        CefrLevel.TryParse(text, out var level)
            ? level
            : throw new InvalidOperationException($"Invalid CEFR level in database: '{text}'.");
}
