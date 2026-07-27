using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Curriculum;

/// <summary>Status of a content package version (ADR-014: published versions are immutable).</summary>
public enum CurriculumVersionStatus
{
    Draft = 1,
    Published = 2,
    Retired = 3,
}

/// <summary>A versioned curriculum content package for one language.</summary>
public sealed class CurriculumVersion : AggregateRoot
{
    private CurriculumVersion()
    {
    }

    private CurriculumVersion(Guid id, LanguageCode language, string version, CurriculumVersionStatus status)
        : base(id)
    {
        Language = language;
        Version = version;
        Status = status;
    }

    public LanguageCode Language { get; private set; }

    /// <summary>Semver of the content package (e.g. <c>0.1.0</c>).</summary>
    public string Version { get; private set; } = string.Empty;

    public CurriculumVersionStatus Status { get; private set; }

    public static CurriculumVersion CreateDraft(LanguageCode language, string version) =>
        new(Guid.CreateVersion7(), language, version, CurriculumVersionStatus.Draft);

    public void Publish() => Status = CurriculumVersionStatus.Published;

    public void Retire() => Status = CurriculumVersionStatus.Retired;
}
