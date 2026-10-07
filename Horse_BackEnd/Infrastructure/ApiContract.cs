using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using System.Text.RegularExpressions;

namespace Horse_BackEnd.Infrastructure;

public sealed class ApiContractTransformer : IOpenApiOperationTransformer
{
    /// <summary>
    /// Bổ sung schema lỗi, bearer security, mã HTTP và hợp đồng multipart/download vào OpenAPI theo metadata endpoint.
    /// </summary>
    /// <param name="operation">Giá trị kiểu OpenApiOperation dùng trong TransformAsync.</param>
    /// <param name="context">Giá trị kiểu OpenApiOperationTransformerContext dùng trong TransformAsync.</param>
    /// <param name="cancellationToken">Giá trị kiểu CancellationToken dùng trong TransformAsync.</param>
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var path = "/" + Regex.Replace(context.Description.RelativePath!, @"\{([^}:]+):[^}]+\}", "{$1}").Trim('/');
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        operation.OperationId = context.Description.HttpMethod!.ToLowerInvariant() + "_" + path.Trim('/').Replace('/', '_').Replace("{", "").Replace("}", "").Replace('-', '_');
        if (!path.StartsWith("/api/", StringComparison.Ordinal)) return;

        var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ApiErrorResponse), null, cancellationToken);
        context.Document!.AddComponent(nameof(ApiErrorResponse), errorSchema);
        var error = new OpenApiSchemaReference(nameof(ApiErrorResponse), context.Document);
        operation.Responses ??= new OpenApiResponses();
        // Bổ sung response lỗi OpenAPI khi mã HTTP chưa có để không ghi đè schema endpoint đã khai báo.
        void AddError(int status, string description) => operation.Responses.TryAdd(status.ToString(), new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Schema = error } }
        });
        AddError(400, "Invalid body, query, enum, or business validation. See title/detail.");
        AddError(409, "State, duplicate data, or concurrent transaction conflict. Reload before retrying.");
        AddError(500, "Unexpected server error. Use traceId for support.");

        var protectedRoute = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        if (protectedRoute)
        {
            context.Document!.Components ??= new OpenApiComponents();
            context.Document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            context.Document.Components.SecuritySchemes.TryAdd("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Description = "Opaque ASP.NET Core Identity access token from /api/auth/login or /refresh. Send Authorization: Bearer <accessToken>."
            });
            operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] }];
            AddError(401, "Authentication required or token/account invalid. Authentication middleware can return an empty body.");
            AddError(403, "Role or record ownership/assignment does not permit this action. Authorization middleware can return an empty body.");
            AddError(404, "Referenced record or file was not found.");
            operation.Description = (operation.Description ?? "") + " Authentication is required. Role and record ownership/assignment checks also apply; see docs/CONTRACT_AND_SCREEN_MAP.md.";
        }
        if (metadata.OfType<EnableRateLimitingAttribute>().Any())
            operation.Responses.TryAdd("429", new OpenApiResponse { Description = "Rate limit reached. Empty response body; retry later." });

        if (path == "/api/auth/login")
        {
            var loginError = await context.GetOrCreateSchemaAsync(typeof(LoginErrorResponse), null, cancellationToken);
            operation.Responses["401"] = new OpenApiResponse
            {
                Description = "Invalid credentials: { error: invalid_credentials }.",
                Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Schema = loginError } }
            };
        }
        if (path == "/api/auth/refresh")
            operation.Responses["401"] = new OpenApiResponse { Description = "Invalid/expired refresh token or revoked account. Empty body." };

        var upload = HttpMethods.IsPost(context.Description.HttpMethod!) && (path == "/api/registrations/{registrationId}/attachments" || path == "/api/care/incidents/{incidentId}/photos");
        if (upload)
        {
            var properties = new Dictionary<string, IOpenApiSchema>
            {
                ["file"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary", Description = "One PNG/JPEG image; registration attachments also allow PDF except HorsePhoto. File extension must match content." }
            };
            var required = new HashSet<string> { "file" };
            if (path.Contains("registrations"))
            {
                properties["type"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "AttachmentType name (case-sensitive): " + string.Join(", ", Enum.GetNames<AttachmentType>().Where(x => x != nameof(AttachmentType.IncidentPhoto))) };
                properties["certificateNumber"] = new OpenApiSchema { Type = JsonSchemaType.String, MaxLength = 100 };
                properties["issueDate"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
                properties["expiryDate"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
                required.Add("type");
            }
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType> { ["multipart/form-data"] = new() { Schema = new OpenApiSchema { Type = JsonSchemaType.Object, Properties = properties, Required = required } } }
            };
            operation.Responses.TryAdd("413", new OpenApiResponse { Description = "Request exceeds the configured server/form size limit; response body can be empty." });
        }
        if (path == "/api/reports") AddError(413, "Report exceeds configured record limit. Narrow horse/date filters.");

        var download = HttpMethods.IsGet(context.Description.HttpMethod!) && (path.EndsWith("/photo", StringComparison.Ordinal)
            || path == "/api/registrations/{registrationId}/attachments/{id}" || path == "/api/care/incidents/{incidentId}/photos/{id}");
        if (download)
        {
            var types = path.Contains("attachments") ? new[] { "image/png", "image/jpeg", "application/pdf" } : new[] { "image/png", "image/jpeg" };
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "File bytes with the stored content type and Content-Disposition filename.",
                Content = types.ToDictionary(x => x, _ => new OpenApiMediaType { Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" } })
            };
        }
    }
}
