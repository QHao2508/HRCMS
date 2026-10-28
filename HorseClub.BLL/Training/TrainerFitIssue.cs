using HorseClub.BLL.Messaging;

namespace HorseClub.BLL.Training;

/// <summary>
/// Một lý do Trainer chưa nhận được ngựa. <see cref="Key"/>/<see cref="Args"/> để client dịch,
/// <see cref="Code"/> là mã lỗi API khi phân công bị chặn.
/// </summary>
public sealed record TrainerFitIssue(MessageKey Key, string Code, object?[]? Args = null)
{
    public MessageText Text => Messages.Text(Key, Args ?? []);
}
