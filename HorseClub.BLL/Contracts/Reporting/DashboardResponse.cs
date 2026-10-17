namespace HorseClub.BLL.Contracts;

public sealed record DashboardResponse(
    int HorseCount,
    int ActivePlans,
    int SessionsToday,
    int OverdueSessions,
    int CareTasksToday,
    int RestrictedHorses,
    int UnreadNotifications);
