using System.ComponentModel.DataAnnotations;
namespace HorseClub.BLL.Contracts;
public sealed record ReplenishmentReceiptRequest([property: Range(typeof(decimal), "0.001", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] decimal Quantity, [property: Required, MaxLength(2000)] string Reason);
public sealed record ReplenishmentReceiptResponse(HorseClub.DAL.Entities.ReplenishmentRequest Request, StockMovementResponse Stock);
