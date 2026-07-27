using Lif.Application.Abstractions;
using Lif.Domain.Learner;
using Lif.SharedKernel.Primitives;
using Lif.SharedKernel.Results;

namespace Lif.Application.Learners;

/// <summary>Beginner fast path (doc 09): no placement, straight to A1.1 with zero mastery.</summary>
public sealed record OnboardLearnerCommand(
    string DisplayName,
    string TargetLevel,
    int MinutesPerDay,
    string? MotivationType = null);

public sealed record OnboardLearnerResult(
    Guid LearnerId,
    string CurrentLevel,
    string TargetLevel,
    int MinutesPerDay,
    Guid CurriculumVersionId);

public sealed class OnboardLearnerHandler(
    ICurriculumReader curriculum,
    ILearnerRepository learners,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<OnboardLearnerResult>> HandleAsync(
        OnboardLearnerCommand command, CancellationToken cancellationToken = default)
    {
        if (!CefrLevel.TryParse(command.TargetLevel, out var targetLevel))
        {
            return Result.Failure<OnboardLearnerResult>(ApplicationErrors.InvalidTargetLevel);
        }

        var version = await curriculum.GetActiveVersionAsync(LanguageCode.German, cancellationToken);
        if (version is null)
        {
            return Result.Failure<OnboardLearnerResult>(ApplicationErrors.CurriculumNotAvailable);
        }

        var profile = LearnerProfile.CreateBeginner(
            command.DisplayName, targetLevel, command.MinutesPerDay, version.Id, command.MotivationType);

        learners.Add(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new OnboardLearnerResult(
            profile.Id,
            profile.CurrentLevel.ToString(),
            profile.TargetLevel.ToString(),
            profile.MinutesPerDay,
            profile.CurriculumVersionId);
    }
}
