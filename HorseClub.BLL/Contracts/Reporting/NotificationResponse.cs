namespace HorseClub.BLL.Contracts;

/// <summary>
/// Thông báo trả cho client: chỉ mã + ngữ cảnh, không có câu chữ. Client dịch MessageCode theo catalog của mình,
/// hiển thị HorseName/Subject và dùng Type + ReferenceId/HorseId để mở đúng màn hình.
/// </summary>
public sealed record NotificationResponse(Guid Id, string Type, string MessageCode, Guid? ReferenceId, Guid? HorseId,
    string? HorseName, string? Subject, bool Read, DateTimeOffset CreatedAt);
