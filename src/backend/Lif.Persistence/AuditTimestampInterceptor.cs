using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lif.Persistence;

/// <summary>
/// Maintains the shadow <c>created_at</c> / <c>updated_at</c> columns present on every table
/// (doc 07 conventions). Registered on every context instance (factory + runtime DI).
/// </summary>
public sealed class AuditTimestampInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Touch(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Touch(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Touch(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Metadata.FindProperty(LifDbContext.CreatedAtProperty) is null)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Property(LifDbContext.CreatedAtProperty).CurrentValue = now;
                entry.Property(LifDbContext.UpdatedAtProperty).CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(LifDbContext.UpdatedAtProperty).CurrentValue = now;
            }
        }
    }
}
