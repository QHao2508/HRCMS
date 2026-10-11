using System.ComponentModel.DataAnnotations;
namespace HorseClub.BLL.Contracts;
public sealed record InvitationVerificationResponse(bool Verified,string? SetupToken=null);
public sealed record InvitationPasswordRequest([property:Required,EmailAddress]string Email,[property:Required,StringLength(4096)]string SetupToken,[property:Required]string Password,[property:Required]string ConfirmPassword);
