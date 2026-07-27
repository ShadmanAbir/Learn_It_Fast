using Lif.Application.Abstractions;
using Lif.Application.Learners;
using Lif.Application.Sessions;
using Lif.SharedKernel.Results;

namespace Lif.Application.Today;

/// <summary>Dashboard bundle for the Today screen (doc 12 §4: screens map to composed queries).</summary>
public sealed record TodayDto(
    LearnerDto Learner,
    int DueReviewCount,
    SessionDto? OpenSession);

public sealed class GetTodayHandler(
    ILearnerRepository learners,
    IStudySessionRepository sessions,
    IReviewItemRepository reviews,
    IClock clock)
{
    public async Task<Result<TodayDto>> HandleAsync(Guid learnerId, CancellationToken cancellationToken = default)
    {
        var profile = await learners.GetAsync(learnerId, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<TodayDto>(ApplicationErrors.LearnerNotFound);
        }

        var dueCount = await reviews.CountDueAsync(learnerId, clock.UtcNow, cancellationToken);
        var openSession = await sessions.GetOpenForLearnerAsync(learnerId, cancellationToken);

        return new TodayDto(
            LearnerMapper.ToDto(profile),
            dueCount,
            openSession is null ? null : SessionMapper.ToDto(openSession));
    }
}
