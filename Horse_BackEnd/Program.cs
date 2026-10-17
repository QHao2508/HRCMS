using HorseClub.BLL;
using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using HorseClub.DAL.Data;
using HorseClub.DAL;
using HorseClub.DAL.Enums;
using Horse_BackEnd.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Horse_BackEnd.Realtime;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(options => options.AddOperationTransformer<ApiContractTransformer>());
builder.Services.AddOptions<SecurityOptions>().BindConfiguration(SecurityOptions.Section).ValidateDataAnnotations().Validate(x => x.PasswordMaxLength >= x.PasswordMinLength && x.RefreshTokenMinutes >= x.AccessTokenMinutes).ValidateOnStart();
builder.Services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.Section).ValidateDataAnnotations()
    .Validate(x => x.IsValid(), "Storage requires Local or AzureBlob with a valid private container and HTTPS service connection.").ValidateOnStart();
builder.Services.AddOptions<BusinessOptions>().BindConfiguration(BusinessOptions.Section).ValidateDataAnnotations().Validate(x => x.DefaultPageSize <= x.MaxPageSize && x.DefaultReportDays <= x.MaxReportDays && x.MinHorseHeightCm <= x.MaxHorseHeightCm && x.MinHorseWeightKg <= x.MaxHorseWeightKg && ClubCalendar.IsValidZone(x.TimeZoneId)).ValidateOnStart();
builder.Services.AddOptions<WorkerOptions>().BindConfiguration(WorkerOptions.Section).ValidateDataAnnotations().ValidateOnStart();
builder.Services.Configure<WebsiteOptions>(builder.Configuration.GetSection("Website"));
builder.Services.AddOptions<BrandingOptions>().BindConfiguration(BrandingOptions.Section).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.Section).ValidateDataAnnotations()
    .Validate(x => Enum.IsDefined(x.Provider))
    .Validate(x => !builder.Configuration.GetValue("Workers:Enabled", true) || x.CanDeliver(builder.Environment.IsDevelopment()), "Enabled workers require Email:FromAddress and valid Email:Smtp settings; DevelopmentFile is allowed only in Development.")
    .ValidateOnStart();
builder.Services.ConfigureHttpJsonOptions(o => { o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)); o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow; });
var storageLimits = builder.Configuration.GetSection(StorageOptions.Section).Get<StorageOptions>() ?? new();
var securityLimits = builder.Configuration.GetSection(SecurityOptions.Section).Get<SecurityOptions>() ?? new();
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = storageLimits.MaxFileBytes + 1024 * 1024;
    // Keep bounded multipart uploads in memory instead of spooling photos to temporary local files.
    o.MemoryBufferThreshold = checked((int)o.MultipartBodyLengthLimit);
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = storageLimits.MaxFileBytes + 1024 * 1024);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentIdentity, HttpCurrentIdentity>();
builder.Services.AddClubDataAccess();
builder.Services.AddClubBusiness();
builder.Services.AddClubRealtime();
builder.Services.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme, o =>
{
    o.BearerTokenExpiration = TimeSpan.FromMinutes(securityLimits.AccessTokenMinutes); o.RefreshTokenExpiration = TimeSpan.FromMinutes(securityLimits.RefreshTokenMinutes);
    o.Events.OnMessageReceived = context =>
    {
        if (RealtimeRegistration.IsHubPath(context.Request.Path) && !context.Request.Headers.ContainsKey("Authorization")
            && context.Request.Query.TryGetValue("access_token", out var token) && token.Count == 1)
            context.Token = token.ToString();
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.AddClubKeyProtection();
var provider = builder.Configuration.GetValue("Database:Provider", DatabaseProvider.SqlServer);
if (provider == DatabaseProvider.SqlServer)
{
    builder.Services.AddDbContext<SqlServerClubDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer") ?? throw new InvalidOperationException(Messages.Get(MessageKey.ConfigureConnectionStringsSqlServer))));
    builder.Services.AddScoped<ClubDbContext>(sp => sp.GetRequiredService<SqlServerClubDbContext>());
}
else throw new InvalidOperationException(Messages.Get(MessageKey.DatabaseProviderMustBeSqlServer));
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => { if (origins.Length > 0) p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod(); }));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = securityLimits.AuthRequestsPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("uploads", context => RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = storageLimits.RequestsPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
    if (builder.Configuration.GetValue("Database:AutoMigrate", builder.Environment.IsDevelopment())) await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ClubStartup>().Initialize(securityLimits);
}
app.UseMiddleware<ErrorMiddleware>();
app.Use(async (context, next) => { context.Response.Headers.XContentTypeOptions = "nosniff"; await next(); });
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(RealtimeRegistration.ValidateOrigin);
app.UseCors(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.UseMiddleware<TransactionMiddleware>();
app.MapHub<ClubHub>("/hubs/club", options => options.CloseOnAuthenticationExpiration = true).WithMetadata(new ReadOnlyOperation());
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "HorseClub API v1");
        options.RoutePrefix = "swagger";
    });
}
app.MapGet("/health", async (ClubStartup startup, CancellationToken token) => await startup.CanConnect(token) ? Results.Ok(new HealthResponse("healthy")) : Results.StatusCode(503)).Produces<HealthResponse>().Produces(503).WithTags("Health");
var api = app.MapGroup("/api").AddEndpointFilter<ValidationFilter>();
api.MapAuth(); api.MapHorses(); api.MapTraining(); api.MapMedical(); api.MapCare(); api.MapInventory(); api.MapAttachments(); api.MapReporting();
api.MapMetadata();
api.MapBranding();
api.MapWebsite();
api.MapIncidentPhotos();
app.Run();
public partial class Program { }
