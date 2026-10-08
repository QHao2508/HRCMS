using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by IncidentPhotoService.</summary>
public interface IIncidentPhotoService
{
    Task<List<IncidentPhotoResponse>> ListPhotos(Guid incidentId);
    Task<OperationResult> UploadPhoto(Guid incidentId, UploadRequest request);
    Task<OperationResult> DownloadPhoto(Guid incidentId, Guid id);
}
