namespace HorseClub.BLL;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký service nghiệp vụ, clock, storage và background worker với vòng đời DI phù hợp để API sử dụng.
    /// </summary>
    /// <param name="services">Giá trị kiểu IServiceCollection dùng trong AddClubBusiness.</param>
    public static IServiceCollection AddClubBusiness(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CurrentUser>();
        services.AddScoped<ClubAccess>();
        services.AddScoped<ClubEvents>();
        services.AddScoped<PageReader>();
        services.AddScoped<UploadStorage>();
        services.AddSingleton<AzureBlobStore>();
        services.AddScoped<BrandingService>();
        services.AddScoped<IBrandingService>(sp => sp.GetRequiredService<BrandingService>());
        services.AddScoped<ClubCalendar>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<IAuthenticationService>(sp => sp.GetRequiredService<AuthenticationService>());
        services.AddScoped<PendingRegistrationCleanup>();
        services.AddScoped<HorseRegistrationService>();
        services.AddScoped<IHorseRegistrationService>(sp => sp.GetRequiredService<HorseRegistrationService>());
        services.AddScoped<HorseAssignmentService>();
        services.AddScoped<IHorseAssignmentService>(sp => sp.GetRequiredService<HorseAssignmentService>());
        services.AddScoped<HorseProfileService>();
        services.AddScoped<IHorseProfileService>(sp => sp.GetRequiredService<HorseProfileService>());
        services.AddScoped<TrainingTemplateService>();
        services.AddScoped<ITrainingTemplateService>(sp => sp.GetRequiredService<TrainingTemplateService>());
        services.AddScoped<TrainingPlanService>();
        services.AddScoped<ITrainingPlanService>(sp => sp.GetRequiredService<TrainingPlanService>());
        services.AddScoped<TrainingSessionService>();
        services.AddScoped<ITrainingSessionService>(sp => sp.GetRequiredService<TrainingSessionService>());
        services.AddScoped<MedicalService>();
        services.AddScoped<IMedicalService>(sp => sp.GetRequiredService<MedicalService>());
        services.AddScoped<CareService>();
        services.AddScoped<ICareService>(sp => sp.GetRequiredService<CareService>());
        services.AddScoped<IncidentPhotoService>();
        services.AddScoped<IIncidentPhotoService>(sp => sp.GetRequiredService<IncidentPhotoService>());
        services.AddScoped<InventoryService>();
        services.AddScoped<IInventoryService>(sp => sp.GetRequiredService<InventoryService>());
        services.AddScoped<AttachmentService>();
        services.AddScoped<IAttachmentService>(sp => sp.GetRequiredService<AttachmentService>());
        services.AddScoped<ReportingService>();
        services.AddScoped<IReportingService>(sp => sp.GetRequiredService<ReportingService>());
        services.AddScoped<MetadataService>();
        services.AddScoped<IMetadataService>(sp => sp.GetRequiredService<MetadataService>());
        services.AddScoped<IClubMailSender, ClubMailSender>();
        services.AddScoped<ClubStartup>();
        services.AddHostedService<EmailWorker>();
        services.AddHostedService<ReminderWorker>();
        services.AddHostedService<AccountCleanupWorker>();
        return services;
    }
}
