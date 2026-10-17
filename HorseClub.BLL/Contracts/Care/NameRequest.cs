using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Contracts;

public sealed record NameRequest([property: Required, MaxLength(100)] string Name);
