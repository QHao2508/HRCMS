using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by AttachmentService.</summary>
public interface IAttachmentService
{
    Task<OperationResult> GetHorsePhoto(Guid horseId);
    Task<List<AttachmentResponse>> ListAttachments(Guid registrationId);
    Task<OperationResult> UploadAttachment(Guid registrationId, UploadRequest request);
    Task<OperationResult> DownloadAttachment(Guid registrationId, Guid id);
}
