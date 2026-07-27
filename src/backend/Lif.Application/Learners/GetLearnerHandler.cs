using Lif.Application.Abstractions;
using Lif.Domain.Learner;
using Lif.SharedKernel.Results;

namespace Lif.Application.Learners;

/// <summary>Profile projection for GET /me (doc 12 §2).</summary>
public sealed record LearnerDto(
    Guid LearnerId,
    string DisplayName,
    string CurrentLevel,
    string TargetLevel,
    int MinutesPerDay,
    Guid CurriculumVersionId,
    string? MotivationType);

internal static class LearnerMapper
{
    public static LearnerDto ToDto(LearnerProfile profile) => new(
        profile.Id,
        profile.DisplayName,
        profile.CurrentLevel.ToString(),
        profile.TargetLevel.ToString(),
        profile.MinutesPerDay,
        profile.CurriculumVersionId,
        profile.MotivationType);
}

public sealed class GetLearnerHandler(ILearnerRepository learners)
{
    public async Task<Result<LearnerDto>> HandleAsync(Guid learnerId, CancellationToken cancellationToken = default)
    {
        var profile = await learners.GetAsync(learnerId, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<LearnerDto>(ApplicationErrors.LearnerNotFound);
        }

        return LearnerMapper.ToDto(profile);
    }
}
