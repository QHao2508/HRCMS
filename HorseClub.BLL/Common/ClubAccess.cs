using HorseClub.BLL.Messaging;
using System.Data;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Common;

public sealed class ClubAccess(ClubDbContext db, CurrentUser current)
{
    /// <summary>
    /// Lấy hồ sơ ngựa trong phạm vi sở hữu hoặc phân công của người đang đăng nhập; chặn truy cập ngoài phạm vi.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="allowArchived">Giá trị kiểu bool dùng trong Horse.</param>
    public async Task<Horse> Horse(Guid id, bool allowArchived = false)
    {
        var user = await current.Get();
        var horse = Ensure.Found(await db.Horses.FindAsync(id));
        if (horse.Archived && !allowArchived) throw new ApiException(409, "horse_archived", Messages.Get(MessageKey.HorseIsArchived));
        if (user.Role == Role.ClubManager) return horse;
        if (user.Role == Role.HorseOwner && horse.OwnerId == user.Id) return horse;
        if (await db.Assignments.AnyAsync(x => x.HorseId == id && x.StaffId == user.Id && x.Active)) return horse;
        // Riders only see horses with sessions specifically assigned to them.
        if (user.Role == Role.WorkRider && await db.Sessions.AnyAsync(x => x.HorseId == id && x.RiderId == user.Id)) return horse;
        throw new ApiException(403, "forbidden", Messages.Get(MessageKey.HorseIsOutsideYourAssignedScope));
    }
    /// <summary>
    /// Xây dựng IQueryable đã giới hạn phạm vi ngựa theo vai trò/danh tính để mọi truy vấn danh sách dùng cùng chính sách.
    /// </summary>
    public async Task<IQueryable<Horse>> Horses()
    {
        var u = await current.Get();
        var q = db.Horses.Where(x => !x.Archived);
        if (u.Role == Role.ClubManager) return q;
        if (u.Role == Role.HorseOwner) return q.Where(x => x.OwnerId == u.Id);
        if (u.Role == Role.WorkRider) return q.Where(x => db.Sessions.Any(s => s.HorseId == x.Id && s.RiderId == u.Id));
        return q.Where(x => db.Assignments.Any(a => a.HorseId == x.Id && a.StaffId == u.Id && a.Active));
    }
    /// <summary>
    /// Xác nhận người dùng là huấn luyện viên hiện đang được phân công cho ngựa trước khi thay đổi huấn luyện.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task Trainer(Guid horseId)
    {
        var u = await current.Get();
        Ensure.Role(u, Role.Trainer);
        await Horse(horseId);
        Ensure.That(await db.Assignments.AnyAsync(x => x.HorseId == horseId && x.StaffId == u.Id && x.Role == Role.Trainer && x.Active),
            Messages.Get(MessageKey.OnlyTheCurrentAssignedTrainerMayChangeTraining), 403, "forbidden");
    }
    /// <summary>
    /// Xác nhận quyền bác sĩ thú y và phạm vi ngựa trước thao tác y tế.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task Vet(Guid horseId)
    {
        Ensure.Role(await current.Get(), Role.Veterinarian);
        await Horse(horseId);
    }
    /// <summary>
    /// Kiểm tra nhân sự tồn tại, hoạt động và có đúng vai trò yêu cầu trước khi phân công.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="role">Role enum chính xác của backend để kiểm quyền/lọc dữ liệu.</param>
    public async Task<User> Staff(Guid id, Role role)
    {
        var user = Ensure.Found(await db.Users.FindAsync(id));
        Ensure.That(user.Active && user.Role == role, Messages.Get(MessageKey.StaffMustBeAnActive, role));
        return user;
    }
    /// <summary>
    /// Kiểm tra quyền chủ sở hữu/quản lý đối với hồ sơ đăng ký để tránh lộ tài liệu intake ngoài phạm vi.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    public async Task<HorseRegistration> Registration(Guid id)
    {
        var u = await current.Get();
        var r = Ensure.Found(await db.Registrations.FindAsync(id));
        Ensure.That(u.Role == Role.ClubManager || (u.Role == Role.HorseOwner && r.OwnerId == u.Id), Messages.Get(MessageKey.PermissionDenied), 403, "forbidden");
        return r;
    }
    /// <summary>
    /// Kiểm tra quyền xem dữ liệu y tế chi tiết của ngựa trước khi trả hồ sơ khám và hạn chế vận động.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    public async Task MedicalDetails(Guid horseId)
    {
        // Manager gets aggregate/operational summaries, not clinical notes.
        await Vet(horseId);
    }
}
