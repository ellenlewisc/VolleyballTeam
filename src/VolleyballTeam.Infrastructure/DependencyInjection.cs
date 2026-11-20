using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VolleyballTeam.Infrastructure.Config;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Data.Interceptors;
using VolleyballTeam.Infrastructure.Seeding;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;

namespace VolleyballTeam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<TimestampInterceptor>();
        services.AddScoped<TenantContext>();
        services.AddScoped<TenantWriteInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) => options
            // Retries cover the free-tier Azure SQL database resuming from auto-pause.
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .AddInterceptors(
                provider.GetRequiredService<TenantWriteInterceptor>(),
                provider.GetRequiredService<TimestampInterceptor>()));

        services.AddScoped<UserClubService>();
        services.AddScoped<TeamService>();

        services.Configure<DevSeedOptions>(configuration.GetSection(DevSeedOptions.SectionName));
        services.AddScoped<DevDataSeeder>();

        return services;
    }
}
