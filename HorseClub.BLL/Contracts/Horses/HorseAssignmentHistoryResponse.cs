using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Contracts;

public sealed record HorseAssignmentHistoryResponse(Guid HorseId, string HorseName, bool Archived, List<StaffAssignment> Assignments);
