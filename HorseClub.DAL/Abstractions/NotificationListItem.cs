namespace HorseClub.DAL.Abstractions;

/// <summary>
/// Dòng thông báo đã kèm ngữ cảnh: ngựa liên quan (suy ra từ ReferenceId) và tên đối tượng khác
/// (hồ sơ đăng ký, vật tư). Tính khi đọc nên áp dụng cả cho thông báo cũ, không lưu trùng tên.
/// </summary>
public sealed record NotificationListItem(Guid Id, NotificationType Type, string MessageCode, Guid? ReferenceId,
    Guid? HorseId, string? HorseName, string? Subject, bool Read, DateTimeOffset CreatedAt);
