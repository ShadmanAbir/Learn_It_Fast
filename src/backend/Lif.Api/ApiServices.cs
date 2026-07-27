using Lif.Application.Attempts;
using Lif.Application.Learners;
using Lif.Application.Reviews;
using Lif.Application.Sessions;
using Lif.Application.Today;

namespace Lif.Api;

/// <summary>Manual handler registration — no mediator; endpoints call use cases directly (doc 14).</summary>
public static class ApiServices
{
    public static IServiceCollection AddUseCaseHandlers(this IServiceCollection services)
    {
        services.AddScoped<OnboardLearnerHandler>();
        services.AddScoped<GetLearnerHandler>();
        services.AddScoped<UpdateGoalsHandler>();
        services.AddScoped<GetTodayHandler>();
        services.AddScoped<PlanSessionHandler>();
        services.AddScoped<GetCurrentSessionHandler>();
        services.AddScoped<SessionLifecycleHandler>();
        services.AddScoped<RecordAttemptHandler>();
        services.AddScoped<GetReviewQueueHandler>();
        services.AddScoped<AnswerReviewHandler>();
        return services;
    }
}
