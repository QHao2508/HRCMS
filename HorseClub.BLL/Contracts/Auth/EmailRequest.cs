using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record EmailRequest([property: Required, EmailAddress] string Email);
