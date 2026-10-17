using System.Net;
using System.Text.RegularExpressions;

namespace HorseClub.BLL.Workers;

/// <summary>Formats the existing plain-text OTP outbox as HTML without storing another copy of the secret.</summary>
public static class OtpEmailRenderer
{
    private static readonly string Template = LoadTemplate();

    /// <summary>
    /// Nhận email OTP đã biết, tách mã/nội dung và chèn vào template HTML đã encode; trả null với email không thuộc mẫu OTP.
    /// </summary>
    /// <param name="subject">Giá trị kiểu string dùng trong Render.</param>
    /// <param name="plainText">Giá trị kiểu string dùng trong Render.</param>
    public static string? Render(string subject, string plainText)
    {
        // Only format known OTP messages. Other email remains plain text.
        var match = Regex.Match(plainText, @"\AMã OTP [^\r\n]*: ([0-9]{6})(?:\r?\n|\z)", RegexOptions.CultureInvariant);
        if (!match.Success) return null;
        var lines = plainText.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0).Skip(1).ToArray();
        var notes = lines.Where(x => x.StartsWith("Mã có hiệu lực", StringComparison.Ordinal)
            || x.StartsWith("Không chia sẻ", StringComparison.Ordinal)).ToArray();
        var disclaimers = lines.Where(x => x.StartsWith("Nếu bạn", StringComparison.Ordinal)).ToArray();
        var content = lines.Except(notes).Except(disclaimers);
        var values = new Dictionary<string, string>
        {
            ["TITLE"] = Encode(subject.StartsWith("HRCMS - ", StringComparison.Ordinal) ? subject[8..] : subject),
            ["CONTENT"] = string.Join("\n", content.Select(x => $"<p style=\"font-size: 16px;\">{Encode(x)}</p>")),
            ["OTP_CODE"] = match.Groups[1].Value,
            ["NOTES"] = string.Join("\n", notes.Select(x => $"<li style=\"margin-bottom: 5px;\">{Encode(x)}</li>")),
            ["DISCLAIMER"] = string.Join(" ", disclaimers.Select(Encode))
        };
        // One pass prevents user text containing template-like braces from being substituted again.
        return Regex.Replace(Template, @"\{\{([A-Z_]+)\}\}", x => values[x.Groups[1].Value]);
    }

    /// <summary>
    /// HTML-encode nội dung động để tên người dùng hoặc dữ liệu email không chèn markup vào template.
    /// </summary>
    /// <param name="text">Giá trị kiểu string dùng trong Encode.</param>
    private static string Encode(string text) => WebUtility.HtmlEncode(text);
    /// <summary>
    /// Đọc template OTP HTML embedded từ assembly để không phụ thuộc file runtime ngoài project.
    /// </summary>
    private static string LoadTemplate()
    {
        using var stream = typeof(OtpEmailRenderer).Assembly.GetManifestResourceStream("HorseClub.BLL.Workers.Templates.OtpEmail.html")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
