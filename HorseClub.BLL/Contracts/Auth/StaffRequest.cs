using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record StaffRequest([property: Required, EmailAddress] string Email, [property: Required, StringLength(80, MinimumLength = 3)] string UserName,
    [property: Required] string FirstName, [property: Required] string LastName, [property: Required] string Phone,
    [property: Required] string Address, [property: JsonRequired] Role Role);
