using System.Net.Http.Json;
using System.Text.Json;

namespace MoneyCare.Web.Services;

/// <summary>
/// Toàn bộ giao tiếp giữa MoneyCare_Web và MoneyCare_API đi qua đây - Controller bên Web KHÔNG được
/// tự tạo HttpClient hay đụng vào ApplicationDbContext/UserManager. Đây là nơi DUY NHẤT biết địa chỉ
/// API (cấu hình ở appsettings.json mục "ApiBaseUrl").
/// </summary>
public class MoneyCareApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public MoneyCareApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Địa chỉ gốc của API (vd để Views tự ghép đường dẫn ảnh: ApiBaseUrl + "/uploads/...").</summary>
    public string BaseUrl => _http.BaseAddress!.ToString().TrimEnd('/');

    public async Task<T?> GetAsync<T>(string duongDan)
    {
        var res = await _http.GetAsync(duongDan);
        await BaoLoiNeuThatBaiAsync(res);
        if (res.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        return await res.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    public async Task<T?> PostAsync<T>(string duongDan, object? body = null)
    {
        var res = body == null ? await _http.PostAsync(duongDan, null) : await _http.PostAsJsonAsync(duongDan, body, JsonOptions);
        await BaoLoiNeuThatBaiAsync(res);
        if (res.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        var text = await res.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, JsonOptions);
    }

    public async Task PostAsync(string duongDan, object? body = null)
    {
        var res = body == null ? await _http.PostAsync(duongDan, null) : await _http.PostAsJsonAsync(duongDan, body, JsonOptions);
        await BaoLoiNeuThatBaiAsync(res);
    }

    public async Task<T?> PutAsync<T>(string duongDan, object body)
    {
        var res = await _http.PutAsJsonAsync(duongDan, body, JsonOptions);
        await BaoLoiNeuThatBaiAsync(res);
        var text = await res.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, JsonOptions);
    }

    public async Task PutAsync(string duongDan, object body)
    {
        var res = await _http.PutAsJsonAsync(duongDan, body, JsonOptions);
        await BaoLoiNeuThatBaiAsync(res);
    }

    public async Task DeleteAsync(string duongDan)
    {
        var res = await _http.DeleteAsync(duongDan);
        await BaoLoiNeuThatBaiAsync(res);
    }

    /// <summary>Gửi 1 file (multipart/form-data) - vd tải ảnh đại diện/ảnh hóa đơn OCR lên.</summary>
    public async Task<T?> PostFileAsync<T>(string duongDan, string tenTruong, Stream noiDungFile, string tenFile)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(noiDungFile);
        content.Add(fileContent, tenTruong, tenFile);

        var res = await _http.PostAsync(duongDan, content);
        await BaoLoiNeuThatBaiAsync(res);
        return await res.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    /// <summary>Tải dữ liệu nhị phân (ảnh QR, file Excel/PDF xuất báo cáo...).</summary>
    public async Task<(byte[] Bytes, string ContentType, string? FileName)> GetBytesAsync(string duongDan)
    {
        var res = await _http.GetAsync(duongDan);
        await BaoLoiNeuThatBaiAsync(res);
        var bytes = await res.Content.ReadAsByteArrayAsync();
        var contentType = res.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = res.Content.Headers.ContentDisposition?.FileNameStar ?? res.Content.Headers.ContentDisposition?.FileName;
        return (bytes, contentType, fileName);
    }

    private static async Task BaoLoiNeuThatBaiAsync(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return;

        var noiDung = await res.Content.ReadAsStringAsync();
        var thongBaoLoi = TrichThongBaoLoi(noiDung) ?? $"Yêu cầu thất bại (mã {(int)res.StatusCode})";
        throw new ApiException(thongBaoLoi, (int)res.StatusCode);
    }

    /// <summary>Đọc thông báo lỗi từ JSON trả về của API: hỗ trợ cả { "loi": "..." } (lỗi nghiệp vụ tự
    /// viết) lẫn { "errors": { "Field": ["..."] } } (lỗi validate tự động của ASP.NET Core).</summary>
    private static string? TrichThongBaoLoi(string jsonBody)
    {
        if (string.IsNullOrWhiteSpace(jsonBody)) return null;
        try
        {
            using var doc = JsonDocument.Parse(jsonBody);
            if (doc.RootElement.TryGetProperty("loi", out var loiEl)) return loiEl.GetString();

            if (doc.RootElement.TryGetProperty("errors", out var errorsEl))
            {
                var cacLoi = errorsEl.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var gop = string.Join(" ", cacLoi);
                if (!string.IsNullOrWhiteSpace(gop)) return gop;
            }

            if (doc.RootElement.TryGetProperty("title", out var titleEl)) return titleEl.GetString();
        }
        catch (JsonException) { /* body không phải JSON hợp lệ - bỏ qua, dùng thông báo mặc định */ }
        return null;
    }
}
