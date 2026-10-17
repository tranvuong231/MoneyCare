// Cung cấp chức năng gửi email thông qua giao thức SMTP.
using System.Net;
using System.Net.Mail;

namespace MoneyCare.Services;

/// <summary>
/// Gửi email THẬT qua giao thức SMTP tiêu chuẩn - hoạt động với bất kỳ nhà cung cấp SMTP nào:
/// Gmail (dùng App Password), Outlook, SendGrid SMTP relay, Mailgun, hoặc SMTP server riêng.
/// Đây KHÔNG phải là code giả lập - chỉ cần điền đúng thông tin SMTP thật vào appsettings.json
/// (mục "EmailSettings") là gửi được email thật ngay, không cần thêm NuGet package nào
/// (dùng System.Net.Mail có sẵn trong .NET).
///
/// Hướng dẫn nhanh lấy App Password của Gmail:
/// 1. Bật xác minh 2 bước cho tài khoản Google: https://myaccount.google.com/security
/// 2. Tạo App Password tại: https://myaccount.google.com/apppasswords
/// 3. Điền SmtpHost=smtp.gmail.com, SmtpPort=587, SmtpUsername=email của bạn,
///    SmtpPassword=App Password vừa tạo (KHÔNG phải mật khẩu Gmail thường).
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> GuiEmailAsync(string toEmail, string tieuDe, string noiDungHtml)
    {
        var host = _config["EmailSettings:SmtpHost"];
        var portStr = _config["EmailSettings:SmtpPort"];
        var username = _config["EmailSettings:SmtpUsername"];
        var password = _config["EmailSettings:SmtpPassword"];
        var senderEmail = _config["EmailSettings:SenderEmail"] ?? username;
        var senderName = _config["EmailSettings:SenderName"] ?? "MoneyCare";
        var enableSsl = _config.GetValue<bool>("EmailSettings:EnableSsl", true);

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Chưa cấu hình SMTP (EmailSettings) - bỏ qua gửi email thật, dùng chế độ demo.");
            return false;
        }

        try
        {
            using var client = new SmtpClient(host, int.Parse(portStr ?? "587"))
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail!, senderName),
                Subject = tieuDe,
                Body = noiDungHtml,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email tới {Email} thất bại", toEmail);
            return false;
        }
    }
}
