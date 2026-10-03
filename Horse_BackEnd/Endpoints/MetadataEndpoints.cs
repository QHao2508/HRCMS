using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class MetadataEndpoints
{
    public static void MapMetadata(this RouteGroupBuilder api)
    {
        api.MapGet("/metadata/enums", async (CurrentUser current) => await MetadataWorkflow.GetMetadataEnums(current)).RequireAuthorization().WithTags("Metadata");
    }
}
