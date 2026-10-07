using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record MovementRequest(decimal Quantity, [property: Required] string Reason);
