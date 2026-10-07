using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(254)] string Email,
    [property: Required, StringLength(80, MinimumLength = 3)] string UserName,
    [property: Required, MaxLength(100)] string FirstName,
    [property: Required, MaxLength(100)] string LastName,
    [property: Required, MaxLength(30)] string Phone,
    [property: Required, MaxLength(500)] string Address,
    [property: Required] string Password,
    [property: Required] string ConfirmPassword, string? NationalId = null);
