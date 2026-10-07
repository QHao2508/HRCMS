namespace HorseClub.DAL.Entities;

public sealed class TrainingTemplate : Entity
{
    public string Name { get; set; } = "";
    public string Goal { get; set; } = "";
    public string Phase { get; set; } = "";
    public decimal DistanceMetres { get; set; }
    public Intensity Intensity { get; set; }
    public string Surface { get; set; } = "";
    public int FrequencyPerWeek { get; set; }
    public string Notes { get; set; } = "";
    public bool Archived { get; set; }
}
