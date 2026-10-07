using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

// Named projections preserve the existing JSON contract.
public sealed record UserResponse(
    Guid Id,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string Phone,
    string Address,
    Role Role,
    bool EmailVerified,
    bool Active);
