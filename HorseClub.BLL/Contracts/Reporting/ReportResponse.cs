using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record ReportResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    ReportGrouping GroupBy,
    bool NoData,
    ReportKpiResponse Kpi,
    IEnumerable<ReportSeriesResponse> Series,
    IEnumerable<ReportTrainingResponse> Training,
    IEnumerable<ReportCareResponse> Care,
    List<MedicalRecord>? Clinical);
