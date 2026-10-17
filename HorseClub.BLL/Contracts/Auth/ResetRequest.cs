using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record ResetRequest([property: Required, EmailAddress] string Email, [property: Required] string Code,
    [property: Required] string Password, [property: Required] string ConfirmPassword);
