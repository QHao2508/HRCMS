using HorseClub.BLL.Realtime;
using HorseClub.BLL.Workers;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Repositories;

namespace Horse_BackEnd.Realtime;

public static class RealtimeRegistration
{
    public static IServiceCollection AddClubRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<RealtimeConnections>();
        services.AddScoped<IRealtimePublisher, SignalRRealtimePublisher>();
        services.AddHostedService<RealtimeDispatchWorker>();
        return services;
    }

    public static bool IsHubPath(PathString path) => path.StartsWithSegments("/hubs/club");

    public static async Task ValidateOrigin(HttpContext context, RequestDelegate next)
    {
        if (IsHubPath(context.Request.Path) && context.Request.Headers.TryGetValue("Origin", out var origin))
        {
            var allowed = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("Cors:Origins").Get<string[]>() ?? [];
            if (origin.Count != 1 || !allowed.Contains(origin.ToString(), StringComparer.OrdinalIgnoreCase))
            { context.Response.StatusCode = 403; return; }
        }
        await next(context);
    }
}
