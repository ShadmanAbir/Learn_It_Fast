using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;
using Lif.SharedKernel.Results;

namespace Lif.Domain.Curriculum;

/// <summary>Lexicon entry: lemma with grammatical metadata and one or more senses (doc 06 §3.1).</summary>
public sealed class VocabEntry : AggregateRoot
{
    private readonly List<WordSense> _senses = [];

    private VocabEntry()
    {
    }

    private VocabEntry(
        Guid id,
        Guid versionId,
        string lemma,
        string partOfSpeech,
        CefrLevel level,
        Difficulty difficulty,
        string? gender,
        string? plural,
        string? ipa,
        int? frequencyRank,
        bool? separable)
        : base(id)
    {
        VersionId = versionId;
        Lemma = lemma;
        PartOfSpeech = partOfSpeech;
        Level = level;
        Difficulty = difficulty;
        Gender = gender;
        Plural = plural;
        Ipa = ipa;
        FrequencyRank = frequencyRank;
        Separable = separable;
    }

    public Guid VersionId { get; private set; }

    public string Lemma { get; private set; } = string.Empty;

    /// <summary>noun | verb | adj | adv | prep | pron | conj | num | particle | phrase</summary>
    public string PartOfSpeech { get; private set; } = string.Empty;

    public CefrLevel Level { get; private set; }

    public Difficulty Difficulty { get; private set; }

    /// <summary>der | die | das — nouns only.</summary>
    public string? Gender { get; private set; }

    public string? Plural { get; private set; }

    public string? Ipa { get; private set; }

    public int? FrequencyRank { get; private set; }

    /// <summary>Verbs only: separable prefix verb.</summary>
    public bool? Separable { get; private set; }

    public IReadOnlyList<WordSense> Senses => _senses.AsReadOnly();

    public static Result<VocabEntry> Create(
        Guid versionId,
        string lemma,
        string partOfSpeech,
        CefrLevel level,
        Difficulty difficulty,
        string? gender = null,
        string? plural = null,
        string? ipa = null,
        int? frequencyRank = null,
        bool? separable = null)
    {
        if (string.IsNullOrWhiteSpace(lemma))
        {
            return Result.Failure<VocabEntry>(CurriculumErrors.LemmaRequired);
        }

        return new VocabEntry(
            Guid.CreateVersion7(), versionId, lemma.Trim(), partOfSpeech, level, difficulty,
            gender, plural, ipa, frequencyRank, separable);
    }

    public WordSense AddSense(string meaningEn, string exampleDe, string exampleEn, string topic)
    {
        var sense = WordSense.Create(Id, _senses.Count + 1, meaningEn, exampleDe, exampleEn, topic);
        _senses.Add(sense);
        return sense;
    }
}
