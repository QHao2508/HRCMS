namespace HorseClub.DAL.Entities;

public sealed class ReplenishmentRequest : Entity
{
    public Guid ItemId { get; set; }
    public Guid RequestedBy { get; set; }
    public decimal Quantity { get; set; }
    public ReplenishmentStatus Status { get; set; } = ReplenishmentStatus.Pending;
    public string Notes { get; set; } = "";
}
