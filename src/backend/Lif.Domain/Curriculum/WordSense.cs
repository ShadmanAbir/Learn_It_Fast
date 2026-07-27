using Lif.SharedKernel.Domain;

namespace Lif.Domain.Curriculum;

/// <summary>One meaning of a vocabulary entry with a contextual example.</summary>
public sealed class WordSense : Entity
{
    private WordSense()
    {
    }

    private WordSense(Guid id, Guid vocabEntryId, int senseOrder, string meaningEn, string exampleDe, string exampleEn, string topic)
        : base(id)
    {
        VocabEntryId = vocabEntryId;
        SenseOrder = senseOrder;
        MeaningEn = meaningEn;
        ExampleDe = exampleDe;
        ExampleEn = exampleEn;
        Topic = topic;
    }

    public Guid VocabEntryId { get; private set; }

    public int SenseOrder { get; private set; }

    public string MeaningEn { get; private set; } = string.Empty;

    public string ExampleDe { get; private set; } = string.Empty;

    public string ExampleEn { get; private set; } = string.Empty;

    /// <summary>Topical pack: food | travel | family | …</summary>
    public string Topic { get; private set; } = string.Empty;

    internal static WordSense Create(Guid vocabEntryId, int senseOrder, string meaningEn, string exampleDe, string exampleEn, string topic) =>
        new(Guid.CreateVersion7(), vocabEntryId, senseOrder, meaningEn, exampleDe, exampleEn, topic);
}
