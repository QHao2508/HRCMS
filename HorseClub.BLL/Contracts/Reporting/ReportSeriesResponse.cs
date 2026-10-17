namespace HorseClub.BLL.Contracts;

public sealed record ReportSeriesResponse(
    string Period,
    int Sessions,
    int Completed,
    decimal PlannedDistanceMetres,
    decimal ActualDistanceMetres);
