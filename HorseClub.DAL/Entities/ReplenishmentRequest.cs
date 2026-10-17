namespace HorseClub.DAL.Entities;

public sealed class ReplenishmentRequest : Entity
{
    public Guid ItemId { get; set; }
    public Guid RequestedBy { get; set; }
    public decimal Quantity { get; set; }
    public ReplenishmentStatus Status { get; set; } = ReplenishmentStatus.Pending;
    public string Notes { get; set; } = "";
    public decimal ReceivedQuantity { get; set; }
    public string? ReviewNotes { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReceivedBy { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
}
