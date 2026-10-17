using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyCare.Api.Dtos;
using MoneyCare.Services;

namespace MoneyCare.Api.Controllers;

// Không yêu cầu đăng nhập - khách vãng lai (màn hình Đăng nhập/Đăng ký) cũng có thể hỏi
[ApiController]
[AllowAnonymous]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly ILlmService _llmService;

    public ChatbotController(IChatbotService chatbotService, ILlmService llmService)
    {
        _chatbotService = chatbotService;
        _llmService = llmService;
    }

    private const string HeThongChatbot =
        "Bạn là trợ lý tài chính cá nhân của ứng dụng MoneyCare, luôn trả lời bằng tiếng Việt, " +
        "ngắn gọn (tối đa khoảng 120 từ), thân thiện, không dùng thẻ HTML hay Markdown. " +
        "Tập trung tư vấn về quản lý chi tiêu, tiết kiệm, ngân sách cá nhân. " +
        "Nếu câu hỏi không liên quan tài chính, vẫn trả lời lịch sự nhưng hướng người dùng quay lại chủ đề tài chính.";

    [HttpGet("goi-y")]
    public IActionResult GoiY() => Ok(_chatbotService.LayCauHoiGoiY());

    [HttpPost("hoi")]
    public async Task<IActionResult> Hoi(HoiRequest req)
    {
        var traLoiAi = await _llmService.HoiAsync(HeThongChatbot, req?.CauHoi ?? "");
        var traLoi = traLoiAi ?? _chatbotService.TraLoi(req?.CauHoi ?? "");
        return Ok(new { traLoi });
    }
}
