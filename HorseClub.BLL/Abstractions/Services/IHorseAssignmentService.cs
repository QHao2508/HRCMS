using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by HorseAssignmentService.</summary>
public interface IHorseAssignmentService
{
    Task<StaffAssignment> Assign(Guid horseId, AssignmentRequest r);
    Task<StaffAssignment> AssignStaff(Guid id, AssignmentRequest request);
}
