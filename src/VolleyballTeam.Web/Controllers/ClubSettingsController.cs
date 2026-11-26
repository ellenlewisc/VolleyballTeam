using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VolleyballTeam.Infrastructure.Services;
using VolleyballTeam.Infrastructure.Tenancy;
using VolleyballTeam.Web.Authorization;
using VolleyballTeam.Web.ViewModels;

namespace VolleyballTeam.Web.Controllers;

[Authorize(Policy = Policies.ClubAdmin)]
[Route("{club:clubslug}/admin/settings")]
public class ClubSettingsController(TenantContext tenant, ClubService clubs) : ClubScopedController(tenant)
{
    [HttpGet("")]
    public IActionResult Index() =>
        View(WithChoices(new ClubSettingsViewModel { Name = CurrentClub.Name, TimeZoneId = CurrentClub.TimeZoneId }));

    [HttpPost("")]
    public async Task<IActionResult> Index(ClubSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await clubs.UpdateSettingsAsync(model.Name, model.TimeZoneId, cancellationToken);
                Flash("Club settings saved.");
                return RedirectToAction(nameof(Index), new { club = ClubSlug });
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        return View(WithChoices(model));
    }

    // A zone set at bootstrap that isn't in the list still shows, so saving the name doesn't change it.
    private ClubSettingsViewModel WithChoices(ClubSettingsViewModel model)
    {
        model.TimeZones = ClubTime.Choices.Any(c => c.Id == CurrentClub.TimeZoneId)
            ? ClubTime.Choices
            : [.. ClubTime.Choices, new TimeZoneChoice(CurrentClub.TimeZoneId, CurrentClub.TimeZoneId)];
        return model;
    }
}
