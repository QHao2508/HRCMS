using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record StaffResponse(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    Role Role,
    bool Active,
    bool EmailVerified);
