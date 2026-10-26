namespace HorseClub.DAL.Entities;

public sealed class Notification : Entity
{
    public Guid RecipientId { get; set; }
    public NotificationType Type { get; set; }
    /// <summary>Tên MessageKey ổn định (ví dụ PreventiveCareIsDue). Client dịch theo mã này.</summary>
    public string MessageCode { get; set; } = "";
    /// <summary>Bản tiếng Anh phía server, chỉ để log/tra cứu; client không dùng để hiển thị.</summary>
    public string Message { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public bool Read { get; set; }
}
