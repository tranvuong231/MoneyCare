// Giao diện định nghĩa các chức năng gửi email.
namespace MoneyCare.Services;

public interface IEmailService
{
    /// <summary>
    /// Gửi email. Trả về true nếu đã cấu hình SMTP và gửi thành công.
    /// Trả về false nếu CHƯA cấu hình SMTP (chế độ demo) - lúc này controller gọi hàm này
    /// nên tự hiển thị nội dung/link trực tiếp trên màn hình cho người dùng thay vì chỉ dựa vào email.
    /// </summary>
    Task<bool> GuiEmailAsync(string toEmail, string tieuDe, string noiDungHtml);
}
