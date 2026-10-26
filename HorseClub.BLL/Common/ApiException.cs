using HorseClub.BLL.Messaging;

namespace HorseClub.BLL.Common;

/// <summary>Lỗi nghiệp vụ trả về client. Client hiển thị theo <see cref="Key"/>; Message tiếng Anh chỉ phục vụ log.</summary>
public sealed class ApiException(int status, string code, MessageText message, Guid? referenceId = null) : Exception(message.ToString())
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public MessageKey Key { get; } = message.Key;
    public string[] Args { get; } = message.Args;
    public Guid? ReferenceId { get; } = referenceId;
}
