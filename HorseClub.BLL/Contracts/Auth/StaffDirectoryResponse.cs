using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record StaffDirectoryResponse(
    Guid Id,
    string FirstName,
    string LastName,
    Role Role);
