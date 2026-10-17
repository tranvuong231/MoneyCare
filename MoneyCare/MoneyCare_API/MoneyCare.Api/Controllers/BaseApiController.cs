using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Models;

namespace MoneyCare.Api.Controllers;

/// <summary>
/// Controller cha dùng chung cho mọi API controller cần đăng nhập.
/// - Đọc UserId từ JWT (claim NameIdentifier) thay vì Cookie Identity như bản Web cũ.
/// - Cung cấp sẵn hàm Loi(...) để trả lỗi đúng định dạng { "loi": "..." } mà ApiClient bên
///   Flutter/Web đang tự động đọc để hiển thị thông báo lỗi cho người dùng.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? throw new InvalidOperationException("Không xác định được người dùng từ token.");

    /// <summary>Trả lỗi nghiệp vụ đúng định dạng ApiClient (Flutter/Web) đang đọc: { "loi": "..." }.</summary>
    protected ObjectResult Loi(string thongBao, int statusCode = 400) =>
        StatusCode(statusCode, new { loi = thongBao });
}
