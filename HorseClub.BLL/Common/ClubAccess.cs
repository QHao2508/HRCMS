using HorseClub.BLL.Messaging;
using System.Data;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Common;

public sealed class ClubAccess(CurrentUser current, IHorseRepository horses, IAccessRepository records, IAssignmentRepository assignments)
{
    public async Task<HorseClub.DAL.Abstractions.HorseScope> Scope()
    {
        var user = await current.Get();
        return user.Role switch
        {
            Role.ClubManager => new(),
            Role.HorseOwner => new(OwnerId: user.Id),
            Role.WorkRider => new(RiderId: user.Id),
            _ => new(StaffId: user.Id)
        };
    }
    /// <summary>
    /// Lấy hồ sơ ngựa trong phạm vi sở hữu hoặc phân công của người đang đăng nhập; chặn truy cập ngoài phạm vi.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="allowArchived">Giá trị kiểu bool dùng trong Horse.</param>
    public async Task<Horse> Horse(Guid id, bool allowArchived = false)
    {
        var user = await current.Get();
        var horse = Ensure.Found(await horses.FindAsync(id));
        if (horse.Archived && !allowArchived) throw new ApiException(409, "horse_archived", Messages.Get(MessageKey.HorseIsArchived));
        if (user.Role == Role.ClubManager) return horse;
        if (user.Role == Role.HorseOwner && horse.OwnerId == user.Id) return horse;
        if (await assignments.IsAssignedAsync(id, user.Id)) return horse;
        // Riders only see horses with sessions specifically assigned to them.
        if (user.Role == Role.WorkRider && await records.HasRiderSessionAsync(id, user.Id)) return horse;
        throw new ApiException(403, "forbidden", Messages.Get(MessageKey.HorseIsOutsideYourAssignedScope));
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
        Ensure.That(await assignments.IsAssignedAsync(horseId, u.Id, Role.Trainer),
            Messages.Get(MessageKey.OnlyTheCurrentAssignedTrainerMayChangeTraining), 403, "forbidden");
    }
    public async Task<Horse> TrainingHistoryHorse(Guid horseId, Guid creatorId)
    {
        var user = await current.Get();
        var horse = Ensure.Found(await horses.FindAsync(horseId));
        if (user.Role == Role.ClubManager || user.Role == Role.HorseOwner && horse.OwnerId == user.Id
            || user.Role == Role.Trainer && creatorId == user.Id) return horse;
        return await Horse(horseId);
    }
    public async Task<bool> IsCurrentHorseReader(Guid horseId)
    {
        try { await Horse(horseId); return true; }
        catch (ApiException e) when (e.Status == 403 || e.Code == "horse_archived") { return false; }
    }
    public async Task<Horse> AssignmentHistoryHorse(Guid horseId)
    {
        var user = await current.Get();
        var horse = Ensure.Found(await horses.FindAsync(horseId));
        Ensure.That(user.Role == Role.ClubManager || user.Role == Role.HorseOwner && horse.OwnerId == user.Id
            || await records.HasHistoricalAssignmentAsync(horseId, user.Id), Messages.Get(MessageKey.HorseIsOutsideYourAssignedScope), 403, "forbidden");
        return horse;
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
        var user = Ensure.Found(await records.FindUserAsync(id));
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
        var r = Ensure.Found(await records.FindRegistrationAsync(id));
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
