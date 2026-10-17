// Bảo vệ các endpoint "nhạy cảm" chỉ nên gọi từ MoneyCare_Web (server tin cậy, không phải client
// công khai) - vd api/auth/google-web, nơi API tin thẳng email do Web gửi lên mà không tự verify lại
// với Google (vì Web đã tự verify qua OAuth server-side rồi). Yêu cầu header "X-Internal-Key" khớp
// với cấu hình InternalApiKey - cả API lẫn Web phải cấu hình CÙNG một giá trị bí mật này.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MoneyCare.Api.Filters;

public class InternalApiKeyFilter : IActionFilter
{
    private readonly IConfiguration _config;

    public InternalApiKeyFilter(IConfiguration config)
    {
        _config = config;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var khoaMongDoi = _config["InternalApiKey"];
        var khoaGuiLen = context.HttpContext.Request.Headers["X-Internal-Key"].ToString();

        if (string.IsNullOrEmpty(khoaMongDoi) || khoaGuiLen != khoaMongDoi)
        {
            context.Result = new ObjectResult(new { loi = "Không có quyền gọi endpoint nội bộ này." }) { StatusCode = 403 };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
