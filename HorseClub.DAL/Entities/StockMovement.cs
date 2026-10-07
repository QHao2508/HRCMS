namespace HorseClub.DAL.Entities;

public sealed class StockMovement : Entity
{
    public Guid ItemId { get; set; }
    public Guid ActorId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = "";
}
