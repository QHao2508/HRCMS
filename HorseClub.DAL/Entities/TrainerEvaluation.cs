namespace HorseClub.DAL.Entities;

public sealed class TrainerEvaluation : Entity
{
    public Guid SessionId { get; set; }
    public Guid TrainerId { get; set; }
    public string Comment { get; set; } = "";
    public bool AdjustFutureSessions { get; set; }
}
