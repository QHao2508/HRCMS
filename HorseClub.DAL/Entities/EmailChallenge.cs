namespace HorseClub.DAL.Entities;

public sealed class EmailChallenge : Entity
{
    public Guid UserId { get; set; }
    public ChallengePurpose Purpose { get; set; }
    public string CodeHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool Consumed { get; set; }
}
