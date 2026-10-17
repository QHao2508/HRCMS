using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record StallRequest(Guid StableId, [property: Required, MaxLength(100)] string Name);
