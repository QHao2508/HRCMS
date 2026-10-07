using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record ReasonRequest([property: Required, MaxLength(2000)] string Reason);
