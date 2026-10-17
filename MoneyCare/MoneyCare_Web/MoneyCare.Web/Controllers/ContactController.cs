using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.ViewModels;
using MoneyCare.Web.Dtos;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

[AllowAnonymous]
public class ContactController : Controller
{
    private readonly MoneyCareApiClient _api;
    private readonly ICurrentUserService _currentUser;

    public ContactController(MoneyCareApiClient api, ICurrentUserService currentUser)
    {
        _api = api;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        var model = new ContactViewModel();

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _currentUser.GetAppUserAsync();
            if (user != null)
            {
                model.HoTen = user.HoTen ?? "";
                model.Email = user.Email ?? "";
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gui(ContactViewModel model)
    {
        if (!ModelState.IsValid) return View("Index", model);

        try
        {
            await _api.PostAsync("/api/contact", new ContactRequest { HoTen = model.HoTen, Email = model.Email, NoiDung = model.NoiDung });
            TempData["ThongBao"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
        }
        catch (ApiException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Index", model);
        }

        return RedirectToAction(nameof(Index));
    }
}
