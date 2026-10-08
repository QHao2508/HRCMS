using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Training;

public sealed class TrainingTemplateService(CurrentUser current, ITrainingRepository repository, IUnitOfWork unitOfWork, PageReader pager, ClubEvents events) : ITrainingTemplateService
{
    /// <summary>
    /// Đọc danh sách có lọc/phân trang giáo án trong TrainingTemplateService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="page">Giá trị kiểu int? dùng trong ListTemplates.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListTemplates.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<TrainingTemplate>> ListTemplates(int? page, int? pageSize)
    { Ensure.Role(await current.Get(), Role.ClubManager, Role.HeadTrainer, Role.Trainer); var (p, size) = pager.Read(page, pageSize); var data = await repository.ListTemplatesAsync(p, size); return new(data.Items, p, size, data.Total); }

    /// <summary>
    /// Tạo mới giáo án trong TrainingTemplateService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> CreateTemplate(TemplateRequest r)
    {
        Ensure.Role(await current.Get(), Role.HeadTrainer);
        var template = new TrainingTemplate(); Apply(template, r); repository.AddTemplate(template);
        await events.Audit(AuditAction.TrainingTemplateCreated, template.Id); await events.RefreshRoles(Role.ClubManager, Role.HeadTrainer, Role.Trainer); await unitOfWork.SaveChangesAsync(); return OperationResult.Created($"/api/training/templates/{template.Id}", template);
    }

    /// <summary>
    /// Cập nhật giáo án trong TrainingTemplateService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingTemplate> UpdateTemplate(Guid id, TemplateRequest r)
    {
        Ensure.Role(await current.Get(), Role.HeadTrainer); var template = Ensure.Found(await repository.FindTemplateAsync(id));
        Ensure.That(!template.Archived, Messages.Get(MessageKey.TemplateIsArchived)); Apply(template, r);
        await events.Audit(AuditAction.TrainingTemplateEdited, id); await events.RefreshRoles(Role.ClubManager, Role.HeadTrainer, Role.Trainer); await unitOfWork.SaveChangesAsync(); return template;
    }

    /// <summary>
    /// Lưu trữ giáo án trong TrainingTemplateService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> ArchiveTemplate(Guid id)
    {
        Ensure.Role(await current.Get(), Role.HeadTrainer); (Ensure.Found(await repository.FindTemplateAsync(id))).Archived = true;
        await events.Audit(AuditAction.TrainingTemplateArchived, id); await events.RefreshRoles(Role.ClubManager, Role.HeadTrainer, Role.Trainer); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Kiểm và áp dụng mục tiêu, giai đoạn, quãng đường, cường độ và tần suất lên giáo án.
    /// </summary>
    /// <param name="t">Giá trị kiểu TrainingTemplate dùng trong Apply.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public static void Apply(TrainingTemplate t, TemplateRequest r)
    { t.Name = r.Name; t.Goal = r.Goal; t.Phase = r.Phase; t.DistanceMetres = r.DistanceMetres; t.Intensity = r.Intensity; t.Surface = r.Surface; t.FrequencyPerWeek = r.FrequencyPerWeek; t.Notes = r.Notes; }

}
