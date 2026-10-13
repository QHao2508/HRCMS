using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Training;

public sealed class TrainingTemplateService(CurrentUser current, ITrainingRepository repository, IUnitOfWork unitOfWork,
    PageReader pager, ClubEvents events, ITrainingSchedulingRepository profiles) : ITrainingTemplateService
{
    public async Task<PageResponse<TrainingTemplate>> ListTemplates(int? page, int? pageSize)
    {
        var user = await current.Get(); Ensure.Role(user, Role.ClubManager, Role.HeadTrainer, Role.Trainer);
        var (p, size) = pager.Read(page, pageSize);
        if (user.Role == Role.Trainer)
        {
            var skills = await profiles.ExpertiseAsync([user.Id]);
            var eligible = (await repository.TemplatesForTrainerAsync(user.Id)).Where(t => TemplateCurriculumPolicy.Matches(t, skills)).ToList();
            return new(eligible.Skip((p - 1) * size).Take(size).ToList(), p, size, eligible.Count);
        }
        var data = await repository.ListTemplatesAsync(p, size);
        return new(data.Items, p, size, data.Total);
    }
    public async Task<List<TrainingTemplate>> Defaults()
    {
        Ensure.Role(await current.Get(), Role.ClubManager, Role.HeadTrainer);
        return await repository.ListDefaultTemplatesAsync();
    }
    public async Task<List<TemplateTrainerCandidate>> Candidates(Guid id)
    {
        Ensure.Role(await current.Get(), Role.HeadTrainer, Role.ClubManager);
        var template = Ensure.Found(await repository.FindTemplateAsync(id));
        Ensure.That(!template.Archived, Messages.Get(MessageKey.TemplateIsArchived));
        var trainers = await profiles.TrainersAsync();
        var skills = await profiles.ExpertiseAsync(trainers.Select(t => t.User.Id).ToArray());
        var assignments = await repository.TemplateAssignmentsAsync(id);
        return trainers.Select(t => new TemplateTrainerCandidate(t.User.Id, (t.User.FirstName + " " + t.User.LastName).Trim(),
            TemplateCurriculumPolicy.RequiredTypes(template).Count > 0 && TemplateCurriculumPolicy.Matches(template, skills.Where(s => s.StaffId == t.User.Id)),
            skills.Where(s => s.StaffId == t.User.Id).Select(s => new ExpertiseInput(s.TrainingType, s.Level)).ToList(),
            assignments.Any(a => a.TrainerId == t.User.Id))).OrderByDescending(t => t.Matches).ThenBy(t => t.Name).ToList();
    }
    public async Task<OperationResult> AssignTrainer(Guid id, TemplateTrainerRequest request)
    {
        var actor = await current.Get(); Ensure.Role(actor, Role.HeadTrainer);
        var template = Ensure.Found(await repository.FindTemplateAsync(id));
        Ensure.That(!template.Archived, Messages.Get(MessageKey.TemplateIsArchived));
        var trainer = Ensure.Found(await profiles.FindStaffAsync(request.TrainerId));
        Ensure.That(trainer.Role == Role.Trainer && trainer.Active && trainer.EmailVerified, Messages.Get(MessageKey.TemplateMustBeAssignedToQualifiedTrainer));
        if (request.Assigned)
        {
            Ensure.That(TemplateCurriculumPolicy.RequiredTypes(template).Count > 0, Messages.Get(MessageKey.InvalidTemplateCurriculum));
            await ValidateTrainer(id, trainer.Id, requireAssignment: false);
        }
        var assignment = await repository.FindTemplateAssignmentAsync(id, trainer.Id);
        if (assignment is null)
        {
            assignment = new() { TemplateId = id, TrainerId = trainer.Id, AssignedBy = actor.Id, Active = request.Assigned };
            repository.AddTemplateAssignment(assignment);
        }
        else { assignment.Active = request.Assigned; assignment.AssignedBy = actor.Id; }
        await events.Audit(AuditAction.TrainingTemplateEdited, id, JsonSerializer.Serialize(new { request.TrainerId, request.Assigned }));
        await events.RefreshRoles(Role.HeadTrainer, Role.Trainer); await unitOfWork.SaveChangesAsync();
        return OperationResult.NoContent();
    }
    public async Task ValidateTrainer(Guid templateId, Guid trainerId, bool requireAssignment = true)
    {
        var template = Ensure.Found(await repository.FindTemplateAsync(templateId));
        if (TemplateCurriculumPolicy.Read(template) is null) return;
        var skills = await profiles.ExpertiseAsync([trainerId]);
        var assignment = requireAssignment ? await repository.FindTemplateAssignmentAsync(templateId, trainerId) : null;
        Ensure.That(TemplateCurriculumPolicy.Matches(template, skills) && (!requireAssignment || assignment is { Active: true }),
            Messages.Get(MessageKey.TemplateMustBeAssignedToQualifiedTrainer), 409, "template_trainer_mismatch");
    }
    public async Task<OperationResult> CreateTemplate(TemplateRequest r)
    {
        Ensure.Role(await current.Get(), Role.HeadTrainer);
        var template = new TrainingTemplate(); Apply(template, r); repository.AddTemplate(template);
        await events.Audit(AuditAction.TrainingTemplateCreated, template.Id); await events.RefreshRoles(Role.ClubManager, Role.HeadTrainer, Role.Trainer);
        await unitOfWork.SaveChangesAsync(); return OperationResult.Created($"/api/training/templates/{template.Id}", template);
    }
    public async Task<TrainingTemplate> UpdateTemplate(Guid id, TemplateRequest r)
    {
        Ensure.Role(await current.Get(), Role.HeadTrainer); var template = Ensure.Found(await repository.FindTemplateAsync(id));
        Ensure.That(!template.IsSystemDefault, Messages.Get(MessageKey.DefaultTemplateReadOnly), 409, "default_template_read_only");
        Ensure.That(!template.Archived, Messages.Get(MessageKey.TemplateIsArchived)); Apply(template, r);
        await events.Audit(AuditAction.TrainingTemplateEdited, id); await events.RefreshRoles(Role.ClubManager, Role.HeadTrainer, Role.Trainer);
        await unitOfWork.SaveChangesAsync(); return template;
    }
    public async Task<OperationResult> ArchiveTemplate(Guid id)
    {
        Ensure.Role(await current.Get(), Role.HeadTrainer); var template = Ensure.Found(await repository.FindTemplateAsync(id));
        Ensure.That(!template.IsSystemDefault, Messages.Get(MessageKey.DefaultTemplateReadOnly), 409, "default_template_read_only");
        template.Archived = true; await events.Audit(AuditAction.TrainingTemplateArchived, id);
        await events.RefreshRoles(Role.ClubManager, Role.HeadTrainer, Role.Trainer); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }
    public static void Apply(TrainingTemplate t, TemplateRequest r)
    {
        TemplateCurriculumPolicy.Validate(r);
        t.Name = r.Name; t.Goal = r.Goal; t.Phase = r.Phase; t.DistanceMetres = r.DistanceMetres; t.Intensity = r.Intensity;
        t.Surface = r.Surface; t.FrequencyPerWeek = r.FrequencyPerWeek; t.Notes = r.Notes;
        if (r.RequiredTrainingTypes is not null)
        {
            t.RequiredTrainingTypesJson = JsonSerializer.Serialize(r.RequiredTrainingTypes, TemplateCurriculumPolicy.Json);
            t.MinimumExpertiseLevel = r.MinimumExpertiseLevel;
            t.CurriculumJson = JsonSerializer.Serialize(r.Curriculum, TemplateCurriculumPolicy.Json);
        }
    }
}
