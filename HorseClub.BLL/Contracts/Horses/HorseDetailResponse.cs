using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Contracts;

public sealed record HorseDetailResponse(
    Horse Horse,
    Measurement? LatestMeasurement,
    List<StaffAssignment> Assignments,
    StallOccupancy? CurrentStall,
    HorsePreferencesResponse Preferences,
    HorsePhotoResponse? Photo = null);
