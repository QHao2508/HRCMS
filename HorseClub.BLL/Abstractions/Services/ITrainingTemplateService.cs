using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by TrainingTemplateService.</summary>
public interface ITrainingTemplateService
{
    Task<PageResponse<TrainingTemplate>> ListTemplates(int? page, int? pageSize);
    Task<OperationResult> CreateTemplate(TemplateRequest r);
    Task<TrainingTemplate> UpdateTemplate(Guid id, TemplateRequest r);
    Task<OperationResult> ArchiveTemplate(Guid id);
}
