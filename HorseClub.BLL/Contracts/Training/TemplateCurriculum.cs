using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record TemplatePlanOption(string Key, string Label, string Goal, string Phase);
public sealed record TemplateSessionOption(string Key, string Label, TrainingType TrainingType, decimal DistanceMetres,
    Intensity Intensity, string Surface, int DurationMinutes, string Target);
public sealed record TemplateCurriculum(List<TemplatePlanOption> PlanOptions, List<TemplateSessionOption> SessionOptions);
public sealed record TemplateTrainerRequest(Guid TrainerId, bool Assigned = true);
public sealed record TemplateTrainerCandidate(Guid Id, string Name, bool Matches, List<ExpertiseInput> Specialties, bool Assigned,
    List<TrainingType> RequiredTrainingTypes, int MinimumExpertiseLevel,
    List<TrainingType> MissingSpecialties, List<TrainingType> BelowLevelSpecialties, string Eligibility);
