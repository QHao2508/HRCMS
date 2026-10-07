namespace HorseClub.BLL.Contracts;

public sealed record HorsePreferencesResponse(
    Guid? PreferredHeadTrainerId,
    Guid? PreferredGroomId,
    Guid? PreferredVeterinarianId);
