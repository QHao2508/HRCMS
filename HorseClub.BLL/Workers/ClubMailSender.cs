using HorseClub.BLL.Messaging;
using System.Net;
using System.Net.Mail;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Workers;

public sealed class ClubMailSender(IOptions<EmailOptions> options, IHostEnvironment env) : IClubMailSender
{
    /// <summary>
    /// Gửi email qua SMTP cấu hình, chuẩn hóa app password Gmail; chỉ provider DevelopmentFile trong Development mới ghi email kiểm thử.
    /// </summary>
    /// <param name="recipient">Giá trị kiểu string dùng trong Send.</param>
    /// <param name="subject">Giá trị kiểu string dùng trong Send.</param>
    /// <param name="body">Giá trị kiểu string dùng trong Send.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    public async Task Send(string recipient, string subject, string body, CancellationToken token)
    {
        var settings = options.Value;
        if (settings.Provider == EmailDeliveryMode.DevelopmentFile && env.IsDevelopment())
        {
            var path = Path.Combine(env.ContentRootPath, "App_Data", "mail"); Directory.CreateDirectory(path);
            await File.WriteAllTextAsync(Path.Combine(path, $"{Guid.NewGuid():N}.eml"), $"To: {recipient}\nSubject: {subject}\n\n{body}", token);
            return;
        }
        var host = settings.Smtp.Host ?? throw new InvalidOperationException(Messages.Get(MessageKey.ConfigureEmailHostBeforeSendingMail));
        using var smtp = new SmtpClient(host, settings.Smtp.Port) { EnableSsl = settings.Smtp.EnableSsl, UseDefaultCredentials = false };
        var password = string.Equals(host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
            ? settings.Smtp.Password?.Replace(" ", "") : settings.Smtp.Password;
        if (!string.IsNullOrWhiteSpace(settings.Smtp.Username)) smtp.Credentials = new NetworkCredential(settings.Smtp.Username, password);
        using var message = CreateMessage(settings, recipient, subject, body);
        await smtp.SendMailAsync(message, token);
    }

    /// <summary>
    /// Tạo thư UTF-8 với người gửi/nhận, plain text và HTML alternate view; SMTP dùng multipart/alternative.
    /// </summary>
    /// <param name="settings">Các giới hạn/chính sách cấu hình áp dụng tại thời điểm chạy.</param>
    /// <param name="recipient">Giá trị kiểu string dùng trong CreateMessage.</param>
    /// <param name="subject">Giá trị kiểu string dùng trong CreateMessage.</param>
    /// <param name="body">Giá trị kiểu string dùng trong CreateMessage.</param>
    public static MailMessage CreateMessage(EmailOptions settings, string recipient, string subject, string body)
    {
        var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress ?? throw new InvalidOperationException(Messages.Get(MessageKey.ConfigureEmailFrom)), settings.FromName),
            Subject = subject, Body = body,
            SubjectEncoding = System.Text.Encoding.UTF8, BodyEncoding = System.Text.Encoding.UTF8
        };
        if (OtpEmailRenderer.Render(subject, body) is { } html)
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, System.Text.Encoding.UTF8, "text/html"));
        message.To.Add(new MailAddress(recipient));
        return message;
    }
}
