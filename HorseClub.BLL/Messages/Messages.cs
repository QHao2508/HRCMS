using System.Globalization;
using System.Text.Json;

namespace HorseClub.BLL.Messaging;

/// <summary>Single catalog for validation, notifications and email templates.</summary>
public static class Messages
{
    private static readonly IReadOnlyDictionary<string, string> Catalog = Load();
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
    /// Đọc catalog JSON embedded của BLL, kiểm đủ key và giữ dữ liệu dùng lại cho các request.
    /// </summary>
    private static IReadOnlyDictionary<string, string> Load()
    {
        using var stream = typeof(Messages).Assembly.GetManifestResourceStream("HorseClub.BLL.Messages.messages.en.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
}
