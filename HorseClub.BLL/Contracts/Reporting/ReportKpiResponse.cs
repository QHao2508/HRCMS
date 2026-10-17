namespace HorseClub.BLL.Contracts;

public sealed record ReportKpiResponse(
    int Sessions,
    int Completed,
    decimal CompletionRate,
    decimal ActualDistanceMetres,
    decimal ActualTimeSeconds,
    int CareTasks,
    int CompletedCare,
    int? MedicalExaminations);
