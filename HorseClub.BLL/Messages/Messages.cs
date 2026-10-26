using System.Globalization;
using System.Text.Json;

namespace HorseClub.BLL.Messaging;

/// <summary>Single catalog for validation, notifications and email templates.</summary>
public static class Messages
{
    private static readonly IReadOnlyDictionary<string, string> Catalog = Load();
    /// <summary>
    /// Khóa chỉ dùng phía server (cấu hình, email, ghi chú nội bộ); không bao giờ gửi cho client nên client không cần bản dịch.
    /// Mọi khóa còn lại là hợp đồng với Frontend và được liệt kê trong docs/contracts/server-message-keys.json.
    /// </summary>
    public static readonly IReadOnlySet<MessageKey> ServerOnly = new HashSet<MessageKey>
    {
        MessageKey.ConfigureConnectionStringsSqlServer, MessageKey.DatabaseProviderMustBeSqlServer,
        MessageKey.ConfigureBothBootstrapManagerEmailAndBootstrapManagerPasswordOrNeither, MessageKey.BootstrapManagerEmailIsInvalid,
        MessageKey.ConfigureEmailHostBeforeSendingMail, MessageKey.ConfigureEmailFrom, MessageKey.HorseClub,
        MessageKey.YourCodeExpiresAtNeverShareThisCode, MessageKey.Skipped,
        MessageKey.VerificationEmailSubject, MessageKey.VerificationEmailBody, MessageKey.PasswordResetEmailSubject,
        MessageKey.PasswordResetEmailBody, MessageKey.StaffInvitationEmailSubject, MessageKey.StaffInvitationEmailBody,
    };
    /// <summary>Số tham số {0}, {1}… của một khóa, dùng cho hợp đồng dịch với client.</summary>
    /// <param name="key">Khóa thông điệp.</param>
    public static int ArgumentCount(MessageKey key)
        => System.Text.RegularExpressions.Regex.Matches(Catalog[key.ToString()], @"\{(\d+)").Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) + 1).DefaultIfEmpty(0).Max();
    /// <summary>
    /// Tra template theo MessageKey và định dạng tham số với culture thống nhất; nội dung nghiệp vụ không hardcoded trong service.
    /// </summary>
    /// <param name="key">Khóa thông điệp enum trong catalog, không phải nội dung hiển thị.</param>
    /// <param name="arguments">Giá trị kiểu object?[] dùng trong Get.</param>
    public static string Get(MessageKey key, params object?[] arguments)
    {
        var template = Catalog[key.ToString()];
        return arguments.Length == 0 ? template : string.Format(CultureInfo.InvariantCulture, template, arguments);
    }
    /// <summary>
    /// Tạo thông điệp mang mã + tham số; dùng cho lỗi API để client dịch theo mã thay vì so khớp câu chữ.
    /// </summary>
    /// <param name="key">Khóa thông điệp trong catalog.</param>
    /// <param name="arguments">Tham số theo thứ tự placeholder.</param>
    public static MessageText Text(MessageKey key, params object?[] arguments) => new(key, arguments);
    /// <summary>
    /// Đọc catalog JSON embedded của BLL, kiểm đủ key và giữ dữ liệu dùng lại cho các request.
    /// </summary>
    private static IReadOnlyDictionary<string, string> Load()
    {
        using var stream = typeof(Messages).Assembly.GetManifestResourceStream("HorseClub.BLL.Messages.messages.en.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
}
