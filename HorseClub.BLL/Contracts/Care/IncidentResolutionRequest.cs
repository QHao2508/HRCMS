using System.ComponentModel.DataAnnotations;
namespace HorseClub.BLL.Contracts;
public sealed record IncidentResolutionRequest([property: Required, MaxLength(2000)] string Resolution);
