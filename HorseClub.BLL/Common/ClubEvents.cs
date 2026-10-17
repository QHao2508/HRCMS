using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using System.Data;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HorseClub.BLL.Common;

public sealed class ClubEvents(IEventRepository repository, CurrentUser current)
{
    public async Task RefreshHorse(Guid horseId)
    {
        foreach (var recipient in await repository.GetHorseAudienceAsync(horseId)) repository.AddDataSignal(recipient);
    }
    public async Task RefreshRegistration(HorseRegistration registration)
    {
        foreach (var recipient in (await repository.GetManagersAsync()).Append(registration.OwnerId).Distinct()) repository.AddDataSignal(recipient);
    }
    public async Task RefreshRoles(params Role[] roles)
    {
        foreach (var recipient in await repository.GetRoleAudienceAsync(roles)) repository.AddDataSignal(recipient);
    }
    /// <summary>
    /// Lưu snapshot kế hoạch/buổi tập/kết quả/đánh giá để có lịch sử thay đổi và xem lại diễn biến.
    /// </summary>
    /// <param name="plan">Giá trị kiểu TrainingPlan dùng trong TrainingHistory.</param>
    /// <param name="session">Giá trị kiểu TrainingSession? dùng trong TrainingHistory.</param>
    /// <param name="result">Giá trị kiểu SessionResult? dùng trong TrainingHistory.</param>
    /// <param name="evaluation">Giá trị kiểu TrainerEvaluation? dùng trong TrainingHistory.</param>
    public async Task TrainingHistory(TrainingPlan plan, TrainingSession? session = null, SessionResult? result = null, TrainerEvaluation? evaluation = null)
    {
        object state = result is not null && session is not null ? new SessionDetailResponse(session, result, evaluation)
            : session is null ? plan : session;
        var snapshot = JsonSerializer.Serialize(state,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } });
        repository.AddRevision(new TrainingRevision { PlanId = plan.Id, SessionId = session?.Id, ActorId = (await current.Get()).Id, Snapshot = snapshot });
    }
    /// <summary>
    /// Xếp sự kiện audit vào DbContext với người thực hiện và đối tượng liên quan; caller lưu cùng thay đổi nghiệp vụ.
    /// </summary>
    /// <param name="action">Giá trị kiểu AuditAction dùng trong Audit.</param>
    /// <param name="referenceId">Giá trị kiểu Guid dùng trong Audit.</param>
    /// <param name="detail">Giá trị kiểu string dùng trong Audit.</param>
    public async Task Audit(AuditAction action, Guid referenceId, string detail = "")
    {
        var u = await current.Get();
        repository.AddAudit(new AuditEvent { ActorId = u.Id, Action = action, ReferenceId = referenceId, Detail = detail });
    }
    /// <summary>
    /// Xếp thông báo theo enum và MessageKey cho người nhận, giữ cùng transaction nghiệp vụ.
    /// </summary>
    /// <param name="recipient">Giá trị kiểu Guid dùng trong Notify.</param>
    /// <param name="type">Giá trị kiểu NotificationType dùng trong Notify.</param>
    /// <param name="message">Giá trị kiểu MessageKey dùng trong Notify.</param>
    /// <param name="reference">Giá trị kiểu Guid? dùng trong Notify.</param>
    public void Notify(Guid recipient, NotificationType type, MessageKey message, Guid? reference = null)
        => repository.AddNotification(new Notification { RecipientId = recipient, Type = type, Message = Messages.Get(message), ReferenceId = reference });
    /// <summary>
    /// Lấy người quản lý hoạt động để định tuyến các yêu cầu cần duyệt.
    /// </summary>
    /// <param name="type">Giá trị kiểu NotificationType dùng trong Managers.</param>
    /// <param name="message">Giá trị kiểu MessageKey dùng trong Managers.</param>
    /// <param name="reference">Giá trị kiểu Guid dùng trong Managers.</param>
    public async Task Managers(NotificationType type, MessageKey message, Guid reference)
    {
        foreach (var id in await repository.GetManagersAsync()) Notify(id, type, message, reference);
    }
    /// <summary>
    /// Lấy nhân viên đang được phân công cho ngựa để định tuyến thông báo đúng người.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="type">Giá trị kiểu NotificationType dùng trong HorseStaff.</param>
    /// <param name="message">Giá trị kiểu MessageKey dùng trong HorseStaff.</param>
    /// <param name="roles">Giá trị kiểu Role[] dùng trong HorseStaff.</param>
    public async Task HorseStaff(Guid horseId, NotificationType type, MessageKey message, params Role[] roles)
    {
        foreach (var id in await repository.GetHorseStaffAsync(horseId, roles))
            Notify(id, type, message, horseId);
    }
}
