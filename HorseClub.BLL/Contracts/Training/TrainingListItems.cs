using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using HorseClub.DAL.Abstractions;
namespace HorseClub.BLL.Contracts;
public sealed record TrainingPlanListItem(Guid Id, Guid HorseId, Guid TrainerId, Guid TemplateId, string Goal, string Phase, DateOnly StartDate, DateOnly EndDate, string Notes, PlanStatus Status, DateTimeOffset CreatedAt, long Version, string HorseName, string TrainerName) {
 public static TrainingPlanListItem From(TrainingPlan p,TrainingNames n)=>new(p.Id,p.HorseId,p.TrainerId,p.TemplateId,p.Goal,p.Phase,p.StartDate,p.EndDate,p.Notes,p.Status,p.CreatedAt,p.Version,n.HorseName,n.TrainerName);
}
public sealed record TrainingSessionListItem(Guid Id,Guid HorseId,Guid PlanId,Guid? RiderId,DateTimeOffset ScheduledAt,TrainingType TrainingType,decimal DistanceMetres,Intensity Intensity,string Surface,string Target,string Notes,SessionStatus Status,DateTimeOffset? StartedAt,DateTimeOffset CreatedAt,long Version,string HorseName,string TrainerName,string? RiderName) {
 public static TrainingSessionListItem From(TrainingSession s,TrainingNames n)=>new(s.Id,s.HorseId,s.PlanId,s.RiderId,s.ScheduledAt,s.TrainingType,s.DistanceMetres,s.Intensity,s.Surface,s.Target,s.Notes,s.Status,s.StartedAt,s.CreatedAt,s.Version,n.HorseName,n.TrainerName,n.RiderName);
}
