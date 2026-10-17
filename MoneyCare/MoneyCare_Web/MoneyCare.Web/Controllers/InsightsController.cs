using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.ViewModels;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[Authorize]
public class InsightsController : Controller
{
    private readonly MoneyCareApiClient _api;

    public InsightsController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var vm = await _api.GetAsync<InsightsViewModel>("/api/insights");
            return View(vm ?? new InsightsViewModel());
        }
        catch (ApiException ex) when (ex.StatusCode == 402)
        {
            TempData["Loi"] = ex.Message;
            return RedirectToAction("Index", "Premium");
        }
    }
}
