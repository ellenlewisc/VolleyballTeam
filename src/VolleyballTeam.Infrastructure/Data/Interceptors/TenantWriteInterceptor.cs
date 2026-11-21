using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VolleyballTeam.Domain;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Infrastructure.Data.Interceptors;

public class TenantWriteInterceptor(TenantContext tenant) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Enforce(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Enforce(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Enforce(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // SavingChanges fires before EF's own DetectChanges, so FK fix-up must happen first.
        context.ChangeTracker.DetectChanges();

        foreach (var entry in context.ChangeTracker.Entries<TenantModel>())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            var entityName = entry.Metadata.ClrType.Name;

            if (tenant.ClubId is not int clubId)
            {
                throw new TenantViolationException($"Cannot save {entityName} without a current club.");
            }

            if (entry.State == EntityState.Added && entry.Entity.ClubId == 0)
            {
                entry.Entity.ClubId = clubId;
            }

            var originalClubId = entry.State == EntityState.Added
                ? entry.Entity.ClubId
                : entry.Property(e => e.ClubId).OriginalValue;

            if (entry.Entity.ClubId != clubId || originalClubId != clubId)
            {
                throw new TenantViolationException($"{entityName} belongs to another club and cannot be saved from club {clubId}.");
            }
        }
    }
}
