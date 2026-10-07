using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record RefreshRequest([property: Required] string RefreshToken);
