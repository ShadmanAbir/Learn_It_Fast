using Lif.Domain.Learner;

namespace Lif.Application.Abstractions;

/// <summary>Append-only mistake log store.</summary>
public interface IMistakeRepository
{
    public void Add(MistakeRecord mistake);
}
