using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Horses;

public sealed class HorseAssignmentService(ClubDbContext db, CurrentUser current, ClubAccess access, ClubEvents events, TimeProvider clock, ClubCalendar calendar)
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
        Ensure.That(r.StartDate != default && r.StartDate <= calendar.Today(clock), Messages.Get(MessageKey.AssignmentsStartTodayOrEarlierFutureSchedulingIsNot));
        if (r.Role == Role.Trainer)
        {
            Ensure.Role(u, Role.HeadTrainer);
            Ensure.That(await db.Assignments.AnyAsync(x => x.HorseId == horseId && x.StaffId == u.Id && x.Active && x.Role == Role.HeadTrainer), Messages.Get(MessageKey.OnlyTheAssignedHeadTrainerCanAssignATrainer), 403, "forbidden");
        }
        else { Ensure.Role(u, Role.ClubManager); Ensure.That(r.Role is Role.HeadTrainer or Role.Groom or Role.Veterinarian, Messages.Get(MessageKey.UnsupportedAdministrativeAssignmentRole)); }
        await access.Staff(r.StaffId, r.Role);
        foreach (var old in await db.Assignments.Where(x => x.HorseId == horseId && x.Role == r.Role && x.Active).ToListAsync())
        {
            Ensure.That(r.StartDate >= old.StartDate, Messages.Get(MessageKey.ReplacementAssignmentCannotPrecedeTheCurrentAssignment));
            old.Active = false; old.EndDate = r.StartDate;
        }
        var a = new StaffAssignment { HorseId = horseId, StaffId = r.StaffId, Role = r.Role, StartDate = r.StartDate, Notes = r.Notes };
        db.Assignments.Add(a);
        events.Notify(r.StaffId, NotificationType.HorseAssignment, MessageKey.YouHaveBeenAssignedToAHorse, horseId);
        events.Notify(horse.OwnerId, NotificationType.HorseAssignment, MessageKey.OfficialHorseStaffAssignmentChanged, horseId);
        await events.Audit(AuditAction.HorseStaffAssigned, horseId, r.Role.ToString());
        await db.SaveChangesAsync(); return a;
    }

    /// <summary>
    /// Phân công nhân viên trong HorseAssignmentService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    public async Task<StaffAssignment> AssignStaff(Guid id, AssignmentRequest request)
    { return await Assign(id, request); }

}
