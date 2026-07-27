using System.Text.Json;
using System.Text.Json.Serialization;
using Lif.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lif.Persistence;

/// <summary>
/// jsonb mapping for write-once snapshot documents (doc 07 §9: session plan, exercise spec,
/// exam sections). Learner state stays relational — never map it through here.
/// </summary>
public static class JsonColumn
{
    /// <summary>Shared options: enums as strings, value objects via dedicated converters.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new DifficultyJsonConverter() },
    };

    public static ValueConverter<T, string> Converter<T>() =>
        new(value => JsonSerializer.Serialize(value, Options),
            text => JsonSerializer.Deserialize<T>(text, Options)!);

    /// <summary>Snapshot comparer: documents are write-once, so JSON equality is sufficient.</summary>
    public static ValueComparer<T> Comparer<T>() =>
        new((left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
            value => JsonSerializer.Serialize(value, Options).GetHashCode(StringComparison.Ordinal),
            value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!);
}

/// <summary>Serializes the authoring <see cref="Difficulty"/> value object as its 1–10 integer.</summary>
public sealed class DifficultyJsonConverter : JsonConverter<Difficulty>
{
    public override Difficulty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Difficulty.From(reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, Difficulty value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
