using Lif.Application.Abstractions;
using Lif.SharedKernel.Results;

namespace Lif.Application.Sessions;

/// <summary>Session lifecycle transitions (doc 12 §3): start, complete activity, complete, abandon.</summary>
public sealed record SessionCommand(Guid LearnerId, Guid SessionId);

public sealed record CompleteActivityCommand(Guid LearnerId, Guid SessionId, Guid ActivityId);

public sealed class SessionLifecycleHandler(
    IStudySessionRepository sessions,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<SessionDto>> StartAsync(SessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetAsync(command.SessionId, cancellationToken);
        if (session is null || session.LearnerId != command.LearnerId)
        {
            return Result.Failure<SessionDto>(ApplicationErrors.SessionNotFound);
        }

        var result = session.Start(clock.UtcNow);
        if (result.IsFailure)
        {
            return Result.Failure<SessionDto>(result.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDto(session);
    }

    public async Task<Result<SessionDto>> CompleteActivityAsync(
        CompleteActivityCommand command, CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetAsync(command.SessionId, cancellationToken);
        if (session is null || session.LearnerId != command.LearnerId)
        {
            return Result.Failure<SessionDto>(ApplicationErrors.SessionNotFound);
        }

        var result = session.CompleteActivity(command.ActivityId);
        if (result.IsFailure)
        {
            return Result.Failure<SessionDto>(result.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDto(session);
    }

    public async Task<Result<SessionDto>> CompleteAsync(SessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetAsync(command.SessionId, cancellationToken);
        if (session is null || session.LearnerId != command.LearnerId)
        {
            return Result.Failure<SessionDto>(ApplicationErrors.SessionNotFound);
        }

        var result = session.Complete(clock.UtcNow);
        if (result.IsFailure)
        {
            return Result.Failure<SessionDto>(result.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDto(session);
    }

    public async Task<Result> AbandonAsync(SessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetAsync(command.SessionId, cancellationToken);
        if (session is null || session.LearnerId != command.LearnerId)
        {
            return Result.Failure(ApplicationErrors.SessionNotFound);
        }

        var result = session.Abandon(clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
