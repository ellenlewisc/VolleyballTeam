using Azure.Communication.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using VolleyballTeam.Infrastructure.Email;
using VolleyballTeam.Infrastructure.Access;
using VolleyballTeam.Infrastructure.Auditing;
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
        services.AddScoped<TenantContext>();
        services.AddScoped<CurrentUser>();
        services.AddScoped<TenantWriteInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) => options
            // Retries cover the free-tier Azure SQL database resuming from auto-pause.
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .AddInterceptors(
                provider.GetRequiredService<TenantWriteInterceptor>(),
                provider.GetRequiredService<SoftDeleteInterceptor>(),
                provider.GetRequiredService<AuditInterceptor>()));

        services.AddScoped<UserClubService>();
        services.AddScoped<TeamSeasonAccessService>();
        services.AddScoped<TeamService>();
        services.AddScoped<SeasonService>();
        services.AddScoped<PlayerService>();
        services.AddScoped<RosterService>();
        services.AddScoped<RosterCsvService>();
        services.AddScoped<MemberStatusService>();
        services.AddScoped<MemberService>();
        services.AddScoped<InvitationService>();

        services.Configure<BootstrapOptions>(configuration.GetSection(BootstrapOptions.SectionName));
        services.AddScoped<BootstrapSeeder>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton(provider => new Lazy<EmailClient>(() =>
        {
            var email = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            return string.IsNullOrWhiteSpace(email.ConnectionString)
                ? throw new InvalidOperationException("Email:ConnectionString is not configured.")
                : new EmailClient(email.ConnectionString);
        }));
        services.AddScoped<EmailSender>();

        return services;
    }
}
