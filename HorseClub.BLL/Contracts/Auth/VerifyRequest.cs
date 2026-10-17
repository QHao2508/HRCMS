using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record VerifyRequest([property: Required, EmailAddress] string Email, [property: Required] string Code);
