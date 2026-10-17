using Microsoft.AspNetCore.Mvc;
using MoneyCare.Web.Services;

namespace MoneyCare.Controllers;

// Không yêu cầu đăng nhập - khách vãng lai (trang Login/Register/Pricing) cũng có thể hỏi.
// Chỉ là lớp mỏng chuyển tiếp AJAX từ site.js sang MoneyCare_API - không có nghiệp vụ ở đây.
public class ChatbotController : Controller
{
    private readonly MoneyCareApiClient _api;

    public ChatbotController(MoneyCareApiClient api)
    {
        _api = api;
    }

    public class HoiRequest
    {
        public string CauHoi { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> Hoi([FromBody] HoiRequest request)
    {
        var ketQua = await _api.PostAsync<Dictionary<string, string>>("/api/chatbot/hoi", new { cauHoi = request?.CauHoi ?? "" });
        return Json(new { traLoi = ketQua != null && ketQua.TryGetValue("traLoi", out var t) ? t : "" });
    }
}
