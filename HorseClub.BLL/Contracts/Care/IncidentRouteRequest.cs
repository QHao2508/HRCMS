using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

/// <summary>Manager chuyển tiếp sự cố sang Trainer hoặc Veterinarian.</summary>
public sealed record IncidentRouteRequest(Role RoutedTo);
