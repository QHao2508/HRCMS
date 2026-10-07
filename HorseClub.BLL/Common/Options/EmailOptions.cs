using System.ComponentModel.DataAnnotations;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Common;

public sealed class EmailOptions
{
    public const string Section = "Email";
    public EmailDeliveryMode Provider { get; set; } = EmailDeliveryMode.Smtp;
    public string? FromAddress { get; set; }
    public string FromName { get; set; } = "HRCMS";
    public SmtpEmailOptions Smtp { get; set; } = new();

    /// <summary>
    /// Kiểm cấu hình đủ khả năng gửi email theo provider/môi trường trước khi worker chạy.
    /// </summary>
    /// <param name="isDevelopment">Giá trị kiểu bool dùng trong CanDeliver.</param>
    public bool CanDeliver(bool isDevelopment) => Provider == EmailDeliveryMode.DevelopmentFile
        ? isDevelopment
          : Provider == EmailDeliveryMode.Smtp && !string.IsNullOrWhiteSpace(Smtp.Host)
              && Smtp.Port is >= 1 and <= 65535
              && !string.IsNullOrWhiteSpace(FromAddress) && new EmailAddressAttribute().IsValid(FromAddress)
              && (!string.Equals(Smtp.Host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
                  || Smtp.EnableSsl && !string.IsNullOrWhiteSpace(Smtp.Username) && !string.IsNullOrWhiteSpace(Smtp.Password));
}
