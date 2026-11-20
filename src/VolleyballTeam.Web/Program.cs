using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VolleyballTeam.Domain.Entities;
using VolleyballTeam.Infrastructure;
using VolleyballTeam.Infrastructure.Data;
using VolleyballTeam.Infrastructure.Seeding;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddIdentity<AppUser, IdentityRole<int>>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Password.RequiredLength = 10;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
});

builder.Services.AddScoped<IAuthorizationHandler, ClubMemberHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.ClubMember, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new ClubMemberRequirement()));

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.ConstraintMap.Add(ClubSlugRouteConstraint.Name, typeof(ClubSlugRouteConstraint));
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DevDataSeeder>().SeedAsync();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();

public partial class Program;
