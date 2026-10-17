namespace MoneyCare.Web.Services;

/// <summary>Lỗi trả về từ MoneyCare_API (đọc từ field "loi" hoặc "errors" trong JSON lỗi) - Controller
/// bên Web bắt exception này để hiển thị lại cho người dùng y hệt cách ModelState.AddModelError cũ.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
