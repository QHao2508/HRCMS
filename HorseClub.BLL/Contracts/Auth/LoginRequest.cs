using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record LoginRequest([property: Required] string Email, [property: Required] string Password);
