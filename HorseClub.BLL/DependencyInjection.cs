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
        services.AddScoped<ClubCalendar>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<PendingRegistrationCleanup>();
        services.AddScoped<HorseRegistrationService>();
        services.AddScoped<HorseAssignmentService>();
        services.AddScoped<HorseProfileService>();
        services.AddScoped<TrainingTemplateService>();
        services.AddScoped<TrainingPlanService>();
        services.AddScoped<TrainingSessionService>();
        services.AddScoped<MedicalService>();
        services.AddScoped<CareService>();
        services.AddScoped<IncidentPhotoService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<AttachmentService>();
        services.AddScoped<ReportingService>();
        services.AddScoped<MetadataService>();
        services.AddScoped<IClubMailSender, ClubMailSender>();
        services.AddScoped<ClubStartup>();
        services.AddHostedService<EmailWorker>();
        services.AddHostedService<ReminderWorker>();
        services.AddHostedService<AccountCleanupWorker>();
        return services;
    }
}
