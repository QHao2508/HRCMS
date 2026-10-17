using System.ComponentModel.DataAnnotations;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record RestrictionRequest(Guid MedicalRecordId, bool TrainingLock, bool BlockAllTraining, Intensity? MaxIntensity,
    [property: Range(0, 100000)] decimal? MaxDistanceMetres, bool NoSprint,
    DateTimeOffset ValidFrom, DateTimeOffset? ValidUntil, [property: Required] string Reason);
