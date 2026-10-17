using System.Net.Http.Headers;

namespace MoneyCare.Web.Services;

/// <summary>
/// Mỗi request từ trình duyệt đã đăng nhập bằng Cookie (ASP.NET Core Cookie Authentication) - JWT của
/// MoneyCare_API được LƯU SẴN trong 1 claim của cookie đó lúc đăng nhập (xem AccountController.Login).
/// Handler này tự đọc claim đó và đính kèm header "Authorization: Bearer {jwt}" vào MỌI request Web
/// gửi tới API, để API luôn biết Web đang gọi thay mặt cho ai - Web không cần tự quản lý token riêng.
/// </summary>
public class JwtForwardingHandler : DelegatingHandler
{
    public const string JwtClaimType = "moneycare_jwt";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;

    public JwtForwardingHandler(IHttpContextAccessor httpContextAccessor, IConfiguration config)
    {
        _httpContextAccessor = httpContextAccessor;
        _config = config;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var jwt = _httpContextAccessor.HttpContext?.User.FindFirst(JwtClaimType)?.Value;
        if (!string.IsNullOrEmpty(jwt))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }

        // Đính kèm khóa nội bộ cho MỌI request Web gửi sang API - API chỉ thực sự kiểm tra khóa này ở
        // các endpoint có [ServiceFilter(typeof(InternalApiKeyFilter))] (vd api/auth/google-web).
        var internalKey = _config["InternalApiKey"];
        if (!string.IsNullOrEmpty(internalKey))
        {
            request.Headers.Add("X-Internal-Key", internalKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
