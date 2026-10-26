using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Horses;

public sealed class HorseAssignmentService(IAssignmentRepository repository, IUnitOfWork unitOfWork, CurrentUser current, ClubAccess access, ClubEvents events, TimeProvider clock, ClubCalendar calendar, ITrainingSchedulingService scheduling) : IHorseAssignmentService
{
    /// <summary>
    /// Kiểm vai trò người phân công, nhân sự được chọn và ngày bắt đầu; kết thúc phân công cũ, ghi phân công mới cùng audit/thông báo.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<StaffAssignment> Assign(Guid horseId, AssignmentRequest r)
    {
        var u = await current.Get(); var horse = await access.Horse(horseId);
        Ensure.That(r.StartDate != default && r.StartDate <= calendar.Today(clock), Messages.Text(MessageKey.AssignmentsStartTodayOrEarlierFutureSchedulingIsNot));
        Ensure.That(r.EndDate is null || r.EndDate >= r.StartDate, Messages.Text(MessageKey.InvalidRequestBody));
        if (r.Role == Role.Trainer)
        {
            // Manager được phân công Trainer thay HeadTrainer (ví dụ HeadTrainer vắng) nhưng bắt buộc ghi lý do vào ghi chú.
            Ensure.Role(u, Role.HeadTrainer);
            Ensure.That(await repository.IsAssignedAsync(horseId, u.Id, Role.HeadTrainer), Messages.Text(MessageKey.OnlyTheAssignedHeadTrainerCanAssignATrainer), 403, "forbidden");
        }
        else { Ensure.Role(u, Role.ClubManager); Ensure.That(r.Role is Role.HeadTrainer or Role.Groom or Role.Veterinarian, Messages.Text(MessageKey.UnsupportedAdministrativeAssignmentRole)); }
        await access.Staff(r.StaffId, r.Role);
        Ensure.That(r.Role == Role.Trainer || r.RequiredTrainingType is null, Messages.Text(MessageKey.InvalidRequestBody));
        var requiredType = r.RequiredTrainingType;
        if (r.Role == Role.Trainer)
        {
            requiredType ??= (await repository.GetActiveAsync(horseId, Role.Trainer)).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefault()?.RequiredTrainingType;
            await scheduling.ValidateAssignment(horseId, r.StaffId, requiredType);
        }
        foreach (var old in await repository.GetActiveAsync(horseId, r.Role))
        {
            Ensure.That(r.StartDate >= old.StartDate, Messages.Text(MessageKey.ReplacementAssignmentCannotPrecedeTheCurrentAssignment));
            if (r.Role == Role.Groom && old.StaffId != r.StaffId)
            {
                var inProgress = await repository.GetInProgressCareAsync(horseId, old.StaffId);
                Ensure.That(inProgress.Count == 0 || r.TakeOverInProgress && !string.IsNullOrWhiteSpace(r.TakeOverReason) && r.TakeOverReason.Trim().Length <= 2000, Messages.Text(MessageKey.FinishCareBeforeReassignment), 409, "care_in_progress");
                foreach (var task in inProgress)
                {
                    task.GroomId = r.StaffId;
                    await events.Audit(AuditAction.CareTaskReassigned, task.Id, System.Text.Json.JsonSerializer.Serialize(new { From = old.StaffId, To = r.StaffId, Reason = r.TakeOverReason?.Trim(), State = "InProgress" }));
                    events.Notify(r.StaffId,NotificationType.CareAssigned,MessageKey.ACareTaskWasAssignedToYou,task.Id);
                }
                foreach (var task in await repository.GetPendingCareAsync(horseId, old.StaffId))
                {
                    task.GroomId = r.StaffId;
                    await events.Audit(AuditAction.CareTaskReassigned, task.Id, System.Text.Json.JsonSerializer.Serialize(new { From = old.StaffId, To = r.StaffId }));
                    events.Notify(r.StaffId, NotificationType.CareAssigned, MessageKey.ACareTaskWasAssignedToYou, task.Id);
                }
            }
            events.Notify(old.StaffId, NotificationType.HorseAssignment, MessageKey.OfficialHorseStaffAssignmentChanged, horseId);
            old.Active = false; old.EndDate = r.StartDate;
        }
        var a = new StaffAssignment { HorseId = horseId, StaffId = r.StaffId, Role = r.Role, StartDate = r.StartDate, EndDate = r.EndDate, Notes = r.Notes, RequiredTrainingType = requiredType };
        repository.Add(a);
        events.Notify(r.StaffId, NotificationType.HorseAssignment, MessageKey.YouHaveBeenAssignedToAHorse, horseId);
        events.Notify(horse.OwnerId, NotificationType.HorseAssignment, MessageKey.OfficialHorseStaffAssignmentChanged, horseId);
        await events.Audit(AuditAction.HorseStaffAssigned, horseId, r.Role.ToString());
        await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return a;
    }

    /// <summary>
    /// Phân công nhân viên trong HorseAssignmentService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    public async Task<StaffAssignment> AssignStaff(Guid id, AssignmentRequest request)
    { return await Assign(id, request); }

}
