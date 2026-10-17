// Handler giả lập phản hồi HTTP: cho phép test MoneyCareApiClient/CurrentUserService mà KHÔNG cần
// MoneyCare_API thật đang chạy - tự định nghĩa response (mã trạng thái + nội dung JSON) muốn trả về
// cho mỗi request, giống như "đóng vai API" trong lúc kiểm thử.
using System.Net;
using System.Text;

namespace MoneyCare.Web.Tests;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _traLoi;

    /// <summary>Đếm số lần request thực sự được gửi đi - dùng để kiểm tra CurrentUserService có cache
    /// đúng trong 1 request hay không (chỉ nên gọi API 1 lần dù được hỏi nhiều lần).</summary>
    public int SoLanGoi { get; private set; }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> traLoi)
    {
        _traLoi = traLoi;
    }

    /// <summary>Tạo nhanh 1 handler luôn trả về CÙNG 1 response cho mọi request.</summary>
    public static FakeHttpMessageHandler TraVeJson(HttpStatusCode maTrangThai, string jsonBody) =>
        new(_ => new HttpResponseMessage(maTrangThai)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SoLanGoi++;
        return Task.FromResult(_traLoi(request));
    }

    public static HttpClient TaoHttpClient(HttpMessageHandler handler, string baseUrl = "https://api.test.local/") =>
        new(handler) { BaseAddress = new Uri(baseUrl) };
}
