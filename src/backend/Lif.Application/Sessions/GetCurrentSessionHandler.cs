using Lif.Application.Abstractions;
using Lif.SharedKernel.Results;

namespace Lif.Application.Sessions;

/// <summary>GET /sessions/current (doc 12 §3): the learner's open (planned or active) session.</summary>
public sealed class GetCurrentSessionHandler(IStudySessionRepository sessions)
{
    public async Task<Result<SessionDto>> HandleAsync(Guid learnerId, CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetOpenForLearnerAsync(learnerId, cancellationToken);
        return session is null
            ? Result.Failure<SessionDto>(ApplicationErrors.SessionNotFound)
            : SessionMapper.ToDto(session);
    }
}
