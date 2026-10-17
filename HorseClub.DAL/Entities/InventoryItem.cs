namespace HorseClub.DAL.Entities;

public sealed class InventoryItem : Entity
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public bool Archived { get; set; }
}
