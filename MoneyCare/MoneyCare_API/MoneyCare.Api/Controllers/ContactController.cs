using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Dtos;
using MoneyCare.Data;
using MoneyCare.Models;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

// Cho phép cả khách vãng lai (chưa đăng nhập) gửi liên hệ, giống bản Web gốc
[ApiController]
[AllowAnonymous]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public ContactController(ApplicationDbContext context, IEmailService emailService, IConfiguration config)
    {
        _context = context;
        _emailService = emailService;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Gui(ContactRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.HoTen) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.NoiDung))
            return BadRequest(new { loi = "Vui lòng nhập đầy đủ thông tin" });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var tinNhan = new ContactMessage { HoTen = req.HoTen, Email = req.Email, NoiDung = req.NoiDung, UserId = userId };
        _context.ContactMessages.Add(tinNhan);
        await _context.SaveChangesAsync();

        var adminEmail = _config["AdminSeed:Email"] ?? _config["SupportEmail"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var noiDungEmail = $@"
                <p>Có tin nhắn liên hệ mới từ MoneyCare:</p>
                <p><strong>Họ tên:</strong> {req.HoTen}</p>
                <p><strong>Email:</strong> {req.Email}</p>
                <p><strong>Nội dung:</strong><br/>{req.NoiDung}</p>";
            await _emailService.GuiEmailAsync(adminEmail, "[MoneyCare] Liên hệ & Đóng góp mới", noiDungEmail);
        }

        return Ok();
    }
}
