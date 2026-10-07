using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record FollowUpRequest(Guid PreviousRecordId, [property: Required] MedicalRequest Examination, bool Clearance, [property: Required] string Outcome);
