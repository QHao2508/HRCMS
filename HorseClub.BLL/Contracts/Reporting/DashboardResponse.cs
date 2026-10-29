namespace HorseClub.BLL.Contracts;

public sealed record DashboardResponse(
    int HorseCount,
    int ActivePlans,
    int SessionsToday,
    int OverdueSessions,
    int CareTasksToday,
    int RestrictedHorses,
    int UnreadNotifications,
    int OpenIncidents = 0,
    int PendingReviews = 0,
    int LowStockItems = 0);
