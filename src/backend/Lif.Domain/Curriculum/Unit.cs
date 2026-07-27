using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Curriculum;

/// <summary>Thematic unit inside a CEFR sub-level; orders lesson templates (doc 07 §2).</summary>
public sealed class Unit : AggregateRoot
{
    private readonly List<LessonTemplate> _lessonTemplates = [];

    private Unit()
    {
    }

    private Unit(Guid id, Guid versionId, CefrLevel level, int orderIndex, string title, string? description, string? cultureNote)
        : base(id)
    {
        VersionId = versionId;
        Level = level;
        OrderIndex = orderIndex;
        Title = title;
        Description = description;
        CultureNote = cultureNote;
    }

    public Guid VersionId { get; private set; }

    public CefrLevel Level { get; private set; }

    public int OrderIndex { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    /// <summary>Landeskunde note surfaced with the unit.</summary>
    public string? CultureNote { get; private set; }

    public IReadOnlyList<LessonTemplate> LessonTemplates => _lessonTemplates.AsReadOnly();

    public static Unit Create(Guid versionId, CefrLevel level, int orderIndex, string title, string? description = null, string? cultureNote = null) =>
        new(Guid.CreateVersion7(), versionId, level, orderIndex, title, description, cultureNote);

    public LessonTemplate AddLessonTemplate(string title, IReadOnlyList<Guid> conceptIds)
    {
        var template = LessonTemplate.Create(Id, _lessonTemplates.Count, title, conceptIds);
        _lessonTemplates.Add(template);
        return template;
    }
}
