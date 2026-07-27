using Lif.SharedKernel.Domain;

namespace Lif.Domain.Curriculum;

/// <summary>Planned teach flow for 1–3 concepts inside a unit.</summary>
public sealed class LessonTemplate : Entity
{
    private readonly List<Guid> _conceptIds = [];

    private LessonTemplate()
    {
    }

    private LessonTemplate(Guid id, Guid unitId, int orderIndex, string title, IReadOnlyList<Guid> conceptIds)
        : base(id)
    {
        UnitId = unitId;
        OrderIndex = orderIndex;
        Title = title;
        _conceptIds.AddRange(conceptIds);
    }

    public Guid UnitId { get; private set; }

    public int OrderIndex { get; private set; }

    public string Title { get; private set; } = string.Empty;

    /// <summary>1–3 concepts taught together.</summary>
    public IReadOnlyList<Guid> ConceptIds => _conceptIds.AsReadOnly();

    internal static LessonTemplate Create(Guid unitId, int orderIndex, string title, IReadOnlyList<Guid> conceptIds) =>
        new(Guid.CreateVersion7(), unitId, orderIndex, title, conceptIds);
}
