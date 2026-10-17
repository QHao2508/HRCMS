using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Contracts;

public sealed record StockMovementResponse(
    InventoryItem Item,
    StockMovement Movement);
