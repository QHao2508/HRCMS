using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;

namespace Horse_BackEnd.Endpoints;

public static class MetadataEndpoints
{
    public static void MapMetadata(this RouteGroupBuilder api)
    {
        api.MapGet("/metadata/enums", async (CurrentUser current) =>
        {
            await current.Get();
            return typeof(Role).Assembly.GetTypes()
                .Where(t => t.IsEnum && t.Namespace == typeof(Role).Namespace && t != typeof(DatabaseProvider) && t != typeof(EmailDeliveryMode) && t != typeof(ChallengePurpose))
                .OrderBy(t => t.Name).ToDictionary(t => t.Name, t => Enum.GetNames(t));
        }).RequireAuthorization().WithTags("Metadata");
    }
}
