namespace HorseClub.DAL.Entities;

public sealed class Incident : Entity
{
    public Guid HorseId { get; set; }
    public Guid ReporterId { get; set; }
    public Guid? SessionId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public IncidentType Type { get; set; }
    public string Description { get; set; } = "";
    public IncidentSeverity Severity { get; set; }
    public Role RoutedTo { get; set; }
    public bool Resolved { get; set; }
}
