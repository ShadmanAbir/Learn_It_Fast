using Lif.Application.Abstractions;
using Lif.SharedKernel.Primitives;
using Lif.SharedKernel.Results;

namespace Lif.Application.Learners;

/// <summary>PUT /me/goals (doc 12 §2): adjust target level and daily minutes.</summary>
public sealed record UpdateGoalsCommand(Guid LearnerId, string TargetLevel, int MinutesPerDay);

public sealed class UpdateGoalsHandler(ILearnerRepository learners, IUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(UpdateGoalsCommand command, CancellationToken cancellationToken = default)
    {
        if (!CefrLevel.TryParse(command.TargetLevel, out var targetLevel))
        {
            return Result.Failure(ApplicationErrors.InvalidTargetLevel);
        }

        var profile = await learners.GetAsync(command.LearnerId, cancellationToken);
        if (profile is null)
        {
            return Result.Failure(ApplicationErrors.LearnerNotFound);
        }

        profile.UpdateGoals(targetLevel, command.MinutesPerDay);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
