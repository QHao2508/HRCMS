using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Horse_BackEnd.Infrastructure;

public sealed class HorseIdentifierSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        var horse = type == typeof(Horse);
        var registration = type == typeof(HorseRegistration) || type == typeof(RegistrationDraftRequest) || type == typeof(RegistrationRequest);
        if (!horse && !registration) return Task.CompletedTask;
        if (schema.Properties?.TryGetValue("registrationNumber", out var alias) == true && alias is OpenApiSchema legacy)
        {
            legacy.Deprecated = true;
            legacy.Description = horse ? "Deprecated alias for systemRegistrationCode, issued by the club."
                : "Deprecated alias for externalRegistrationNumber, copied from the horse's documents. If both are supplied they must match.";
        }
        if (schema.Properties?.TryGetValue("externalRegistrationNumber", out var external) == true && external is OpenApiSchema document)
            document.Description = "Registration number on the horse's external documents. Optional; never replaced with the system code.";
        if (schema.Properties?.TryGetValue("systemRegistrationCode", out var system) == true && system is OpenApiSchema code)
        {
            code.Description = "Unique club-issued horse code, allocated on approval. Not editable by clients.";
            code.ReadOnly = true;
        }
        return Task.CompletedTask;
    }
}
