using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Common;

/// <summary>
/// Ma trận quyền theo vai trò đã chốt (13/10/2026). Phạm vi dữ liệu (ngựa nào) vẫn do ClubAccess quyết định;
/// file này chỉ trả lời "vai trò nào được dùng chức năng nào" để sửa quyền ở một chỗ.
/// </summary>
public static class RolePolicy
{
    // Sự cố
    public static readonly Role[] IncidentReaders = [Role.ClubManager, Role.HeadTrainer, Role.Trainer, Role.WorkRider, Role.Veterinarian, Role.Groom, Role.HorseOwner];
    public static readonly Role[] IncidentReporters = [Role.Groom, Role.WorkRider, Role.Trainer, Role.Veterinarian];
    /// <summary>Manager, HeadTrainer và Chủ ngựa thấy mọi sự cố của ngựa trong phạm vi; role khác chỉ thấy sự cố mình báo hoặc được chuyển tới.</summary>
    public static bool SeesAllIncidentsInScope(Role role) => role is Role.ClubManager or Role.HeadTrainer or Role.HorseOwner;
    /// <summary>Manager đóng mọi sự cố; HeadTrainer đóng sự cố chuyển cho Trainer; role được chuyển tới đóng sự cố của mình.</summary>
    public static bool CanResolveIncident(Role role, Role routedTo) => role == Role.ClubManager || role == routedTo || role == Role.HeadTrainer && routedTo == Role.Trainer;
    public static bool CanViewIncidentPhotos(Role role) => role is Role.ClubManager or Role.HeadTrainer or Role.HorseOwner;

    // Chăm sóc
    public static readonly Role[] CareReaders = [Role.ClubManager, Role.HeadTrainer, Role.Trainer, Role.Veterinarian, Role.Groom, Role.HorseOwner];

    // Y tế
    /// <summary>Hồ sơ khám đầy đủ: Vet ghi, Manager chỉ đọc.</summary>
    public static readonly Role[] MedicalRecordReaders = [Role.Veterinarian, Role.ClubManager];
    /// <summary>Tóm tắt y tế (tình trạng, chẩn đoán, phác đồ, hạn chế) không kèm ghi chú nội bộ.</summary>
    public static readonly Role[] MedicalOverviewReaders = [Role.Veterinarian, Role.ClubManager, Role.HeadTrainer, Role.HorseOwner];
    public static bool SeesClinicalNotes(Role role) => role is Role.Veterinarian or Role.ClubManager;

    // Số đo
    public static readonly Role[] MeasurementWriters = [Role.ClubManager, Role.Veterinarian, Role.Groom];

    // Danh bạ: Chủ ngựa chỉ thấy các vai trò được chọn ưu tiên khi đăng ký.
    public static readonly Role[] OwnerDirectoryRoles = [Role.HeadTrainer, Role.Groom, Role.Veterinarian];
}
