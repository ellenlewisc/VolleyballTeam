using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Domain.Enums;
using VolleyballTeam.Infrastructure;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Seeding;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.Security;
using VolleyballTeam.Web.Tenancy;

var builder = WebApplication.CreateBuilder(args);
var secureCookies = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddIdentity<AppUser, IdentityRole<int>>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Password.RequiredLength = 10;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager<AppSignInManager>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
    options.Cookie.Name = "vbt.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// Deactivated accounts and changed passwords sign out other sessions within this window.
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(5));

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "vbt.xsrf";
    options.Cookie.SecurePolicy = secureCookies;
});

// App Service terminates TLS at its front end; trust only the one hop it adds.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Login, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddScoped<IAuthorizationHandler, ClubMemberHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TeamSeasonAccessHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.ClubMember, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new ClubMemberRequirement()))
    .AddPolicy(Policies.ClubAdmin, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new ClubMemberRequirement(ClubRole.Owner, ClubRole.Admin)));

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.ConstraintMap.Add(ClubSlugRouteConstraint.Name, typeof(ClubSlugRouteConstraint));
});

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BootstrapSeeder>().SeedAsync();
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();

public partial class Program;
