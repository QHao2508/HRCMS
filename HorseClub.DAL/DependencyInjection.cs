using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using HorseClub.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HorseClub.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddClubDataAccess(this IServiceCollection services)
    {
        services.AddScoped<HorseClub.DAL.Abstractions.IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<HorseClub.DAL.Abstractions.IInventoryRepository, HorseClub.DAL.Repositories.InventoryRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IRegistrationRepository, HorseClub.DAL.Repositories.RegistrationRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IAssignmentRepository, HorseClub.DAL.Repositories.AssignmentRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.ITrainingRepository, HorseClub.DAL.Repositories.TrainingRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IHorseRepository, HorseClub.DAL.Repositories.HorseRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IAccessRepository, HorseClub.DAL.Repositories.AccessRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IEventRepository, HorseClub.DAL.Repositories.EventRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IMedicalRepository, HorseClub.DAL.Repositories.MedicalRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.ICareRepository, HorseClub.DAL.Repositories.CareRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IFileRepository, HorseClub.DAL.Repositories.FileRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IAuthRepository, HorseClub.DAL.Repositories.AuthRepository>();
        services.AddScoped<HorseClub.DAL.Abstractions.IReportingQueries, HorseClub.DAL.Queries.ReportingQueries>();
        services.AddScoped<HorseClub.DAL.Abstractions.IWorkerRepository, HorseClub.DAL.Repositories.WorkerRepository>();
        services.AddScoped<IManagementRepository,ManagementRepository>();
        services.AddScoped<IRealtimeOutboxRepository, RealtimeOutboxRepository>();
        return services;
    }
}
