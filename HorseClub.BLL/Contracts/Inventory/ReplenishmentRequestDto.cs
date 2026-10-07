using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record ReplenishmentRequestDto(Guid ItemId, [property: Range(0.001, 1000000)] decimal Quantity, string Notes);
