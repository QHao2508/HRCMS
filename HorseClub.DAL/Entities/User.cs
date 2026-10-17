namespace HorseClub.DAL.Entities;

public sealed class User : Entity
{
    public string Email { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? NationalIdProtected { get; set; }
    public Role Role { get; set; }
    public bool EmailVerified { get; set; }
    public bool Active { get; set; } = true;
    public int FailedLogins { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
}
