using HorseClub.BLL.Messaging;
using Horse_BackEnd.Contracts;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Endpoints;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(options => options.AddOperationTransformer<ApiContractTransformer>());
builder.Services.AddOptions<SecurityOptions>().BindConfiguration(SecurityOptions.Section).ValidateDataAnnotations().Validate(x => x.PasswordMaxLength >= x.PasswordMinLength && x.RefreshTokenMinutes >= x.AccessTokenMinutes).ValidateOnStart();
builder.Services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.Section).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<BusinessOptions>().BindConfiguration(BusinessOptions.Section).ValidateDataAnnotations().Validate(x => x.DefaultPageSize <= x.MaxPageSize && x.DefaultReportDays <= x.MaxReportDays && ClubCalendar.IsValidZone(x.TimeZoneId)).ValidateOnStart();
builder.Services.AddOptions<WorkerOptions>().BindConfiguration(WorkerOptions.Section).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.Section).ValidateDataAnnotations()
    .Validate(x => Enum.IsDefined(x.Mode))
    .Validate(x => !builder.Configuration.GetValue("Workers:Enabled", true) || x.CanDeliver(builder.Environment.IsDevelopment()), "Enabled workers require SMTP Host/From; DevelopmentFile is allowed only in Development.")
    .ValidateOnStart();
builder.Services.ConfigureHttpJsonOptions(o => { o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)); o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow; });
var storageLimits = builder.Configuration.GetSection(StorageOptions.Section).Get<StorageOptions>() ?? new();
var securityLimits = builder.Configuration.GetSection(SecurityOptions.Section).Get<SecurityOptions>() ?? new();
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = storageLimits.MaxFileBytes + 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = storageLimits.MaxFileBytes + 1024 * 1024);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<WriteGate>();
builder.Services.AddScoped<CurrentUser>(); builder.Services.AddScoped<ClubAccess>(); builder.Services.AddScoped<ClubEvents>();
builder.Services.AddScoped<PageReader>();
builder.Services.AddScoped<UploadStorage>();
builder.Services.AddScoped<ClubCalendar>();
builder.Services.AddScoped<AuthenticationService>(); builder.Services.AddScoped<HorseService>(); builder.Services.AddScoped<TrainingService>();
builder.Services.AddScoped<IClubMailSender, ClubMailSender>();
builder.Services.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme, o =>
{ o.BearerTokenExpiration = TimeSpan.FromMinutes(securityLimits.AccessTokenMinutes); o.RefreshTokenExpiration = TimeSpan.FromMinutes(securityLimits.RefreshTokenMinutes); });
builder.Services.AddAuthorization();
builder.AddClubKeyProtection();
var provider = builder.Configuration.GetValue("Database:Provider", DatabaseProvider.Sqlite);
if (provider == DatabaseProvider.SqlServer)
{
    builder.Services.AddDbContext<SqlServerClubDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer") ?? throw new InvalidOperationException(Messages.Get(MessageKey.ConfigureConnectionStringsSqlServer))));
    builder.Services.AddScoped<ClubDbContext>(sp => sp.GetRequiredService<SqlServerClubDbContext>());
}
else if (provider == DatabaseProvider.Sqlite)
{
    var databasePath = Path.Combine(builder.Environment.ContentRootPath, "App_Data"); Directory.CreateDirectory(databasePath);
    builder.Services.AddDbContext<SqliteClubDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? $"Data Source={Path.Combine(databasePath, "horseclub.db")}"));
    builder.Services.AddScoped<ClubDbContext>(sp => sp.GetRequiredService<SqliteClubDbContext>());
}
else throw new InvalidOperationException(Messages.Get(MessageKey.DatabaseProviderMustBeSqliteOrSqlServer));
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
builder.Services.AddHostedService<EmailWorker>(); builder.Services.AddHostedService<ReminderWorker>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
    if (builder.Configuration.GetValue("Database:AutoMigrate", builder.Environment.IsDevelopment())) await db.Database.MigrateAsync();
    var email = builder.Configuration["Bootstrap:ManagerEmail"];
    var password = builder.Configuration["Bootstrap:ManagerPassword"];
    if (string.IsNullOrWhiteSpace(email) != string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException(Messages.Get(MessageKey.ConfigureBothBootstrapManagerEmailAndBootstrapManagerPasswordOrNeither));
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && !await db.Users.AnyAsync(x => x.Role == Role.ClubManager))
    {
        AuthenticationService.CheckPassword(password, securityLimits);
        Ensure.That(new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email), Messages.Get(MessageKey.BootstrapManagerEmailIsInvalid));
        var manager = new User { Email = AuthenticationService.Normalize(email), UserName = builder.Configuration["Bootstrap:ManagerUserName"] ?? "clubmanager", FirstName = builder.Configuration["Bootstrap:ManagerFirstName"] ?? "Club", LastName = builder.Configuration["Bootstrap:ManagerLastName"] ?? "Manager", Role = Role.ClubManager, EmailVerified = true };
        manager.PasswordHash = new PasswordHasher<User>().HashPassword(manager, password); db.Users.Add(manager); await db.SaveChangesAsync();
    }
}
app.UseMiddleware<ErrorMiddleware>();
app.Use(async (context, next) => { context.Response.Headers.XContentTypeOptions = "nosniff"; await next(); });
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseCors(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.UseMiddleware<TransactionMiddleware>();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", async (ClubDbContext db) => await db.Database.CanConnectAsync() ? Results.Ok(new HealthResponse("healthy")) : Results.StatusCode(503)).Produces<HealthResponse>().Produces(503).WithTags("Health");
var api = app.MapGroup("/api").AddEndpointFilter<ValidationFilter>();
api.MapAuth(); api.MapHorses(); api.MapTraining(); api.MapMedical(); api.MapCare(); api.MapInventory(); api.MapAttachments(); api.MapReporting();
api.MapMetadata();
api.MapIncidentPhotos();
app.Run();
public partial class Program { }
