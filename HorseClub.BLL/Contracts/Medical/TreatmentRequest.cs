using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record TreatmentRequest(Guid MedicalRecordId, Guid? InjuryId, DateOnly StartDate, DateOnly EndDate,
    DateOnly FollowUpDate, [property: Required] string Objective, [property: Required] string Instructions,
    string Medication, [property: Required] string Frequency);
