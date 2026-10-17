namespace HorseClub.DAL.Entities;

public sealed class SessionResult : Entity
{
    public Guid SessionId { get; set; }
    public decimal DistanceMetres { get; set; }
    public decimal TimeSeconds { get; set; }
    public decimal SpeedMetresPerSecond { get; set; }
    public int? HeartRate { get; set; }
    public Intensity Intensity { get; set; }
    public string Feedback { get; set; } = "";
    public bool AbnormalObservation { get; set; }
}
