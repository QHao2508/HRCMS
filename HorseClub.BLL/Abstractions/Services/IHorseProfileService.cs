using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by HorseProfileService.</summary>
public interface IHorseProfileService
{
    Task<PageResponse<Horse>> ListHorses(string? search, HealthStatus? healthStatus, int? page, int? pageSize);
    Task<HorseDetailResponse> GetHorse(Guid id);
    Task<PageResponse<Measurement>> ListMeasurements(Guid id, int? page, int? pageSize);
    Task<Measurement> AddMeasurement(Guid id, MeasurementRequest request);
    Task<OperationResult> ArchiveHorse(Guid id);
}
