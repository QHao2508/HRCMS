using System.Globalization;

namespace HorseClub.BLL.Messaging;

/// <summary>
/// Mã thông điệp kèm tham số. API trả <see cref="Key"/> và <see cref="Args"/> để Frontend tự dịch;
/// <see cref="ToString"/> chỉ dùng cho log, email và nội dung lưu trữ phía server.
/// </summary>
public readonly record struct MessageText(MessageKey Key, object?[]? Arguments)
{
    /// <summary>Tham số đã định dạng culture-invariant theo đúng thứ tự placeholder {0}, {1}… trong catalog.</summary>
    public string[] Args => (Arguments ?? []).Select(value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").ToArray();
    public override string ToString() => Messages.Get(Key, Arguments ?? []);
    public static implicit operator string(MessageText text) => text.ToString();
}
