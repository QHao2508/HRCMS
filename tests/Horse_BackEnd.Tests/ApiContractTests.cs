using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using HorseClub.DAL.Enums;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class ApiContractTests
{
    [Fact]
    public async Task MalformedAndMissingNestedRequestsReturn400BeforeBusinessWrites()
    {
        await using var factory = new ClubFactory();
        var client = await factory.Client();
        var requests = new[]
        {
            ("/api/auth/login", """{"email":"manager@example.test","password":"x","extra":"unexpected"}"""),
            ("/api/staff", """{"email":"test@example.test","userName":"test","firstName":"Test","lastName":"Test","phone":"0900","address":"Here","role":1}"""),
            ($"/api/horses/{Guid.NewGuid()}/medical/follow-ups", """{"previousRecordId":"00000000-0000-0000-0000-000000000001","examination":null,"clearance":false,"outcome":"Check"}"""),
            ($"/api/horses/{Guid.NewGuid()}/medical/follow-ups", """{"previousRecordId":"00000000-0000-0000-0000-000000000001","examination":{"examinationAt":"2026-10-01T00:00:00Z","reason":"","symptoms":"Normal","findings":"Normal","diagnosis":"Check","healthStatus":"Fit","notes":""},"clearance":false,"outcome":"Check"}""")
        };
        foreach (var (path, json) in requests)
        {
            var response = await client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, error.GetProperty("status").GetInt32());
        }
    }

    [Fact]
    public async Task OpenApiCoversSuccessSchemasSecurityUploadsAndActualStatusCodes()
    {
        await using var factory = new ClubFactory();
        var client = factory.CreateClient();
        var text = await client.GetStringAsync("/openapi/v1.json");
        var export = Environment.GetEnvironmentVariable("HRCMS_EXPORT_OPENAPI");
        if (!string.IsNullOrWhiteSpace(export)) await File.WriteAllTextAsync(export, text);
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var ids = new HashSet<string>();
        var operationCount = 0;
        foreach (var path in paths.EnumerateObject())
            foreach (var operation in path.Value.EnumerateObject().Where(x => x.Name is "get" or "post" or "put" or "delete" or "patch"))
            {
                operationCount++;
                Assert.True(ids.Add(operation.Value.GetProperty("operationId").GetString()!), path.Name);
                var responses = operation.Value.GetProperty("responses");
                var successes = responses.EnumerateObject().Where(x => x.Name.StartsWith('2')).ToList();
                Assert.Single(successes);
                var success = successes[0];
                if (success.Name == "204") Assert.False(success.Value.TryGetProperty("content", out _));
                else
                {
                    var content = success.Value.GetProperty("content");
                    Assert.NotEmpty(content.EnumerateObject());
                    foreach (var media in content.EnumerateObject())
                        Assert.NotEmpty(media.Value.GetProperty("schema").EnumerateObject());
                }
                var anonymous = path.Name is "/health" or "/api/branding/logo" || path.Name.StartsWith("/api/auth/") && path.Name is not "/api/auth/me" and not "/api/auth/logout";
                var secured = operation.Value.TryGetProperty("security", out var security) && security.GetArrayLength() > 0;
                Assert.Equal(!anonymous, secured);
                if (!anonymous) Assert.True(responses.TryGetProperty("401", out _));
            }
        Assert.Equal(97, operationCount);
        Assert.Equal("bearer", root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        foreach (var path in new[] { "/api/registrations/{registrationId}/attachments", "/api/care/incidents/{incidentId}/photos" })
        {
            var post = paths.GetProperty(path).GetProperty("post");
            var schema = post.GetProperty("requestBody").GetProperty("content").GetProperty("multipart/form-data").GetProperty("schema");
            Assert.Contains("file", schema.GetProperty("required").EnumerateArray().Select(x => x.GetString()));
            Assert.Equal("binary", schema.GetProperty("properties").GetProperty("file").GetProperty("format").GetString());
            Assert.True(post.GetProperty("responses").TryGetProperty("201", out _));
        }
        Assert.True(paths.GetProperty("/api/auth/register").GetProperty("post").GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(paths.GetProperty("/api/auth/logout").GetProperty("post").GetProperty("responses").TryGetProperty("204", out _));
    }

    [Fact]
    public async Task AccountPaginationAndErrorShapesRemainCompatible()
    {
        await using var factory = new ClubFactory();
        var client = await factory.Client();
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("ClubManager", me.GetProperty("role").GetString());
        Assert.Equal(new[] { "active", "address", "email", "emailVerified", "firstName", "id", "lastName", "phone", "role", "userName" }, me.EnumerateObject().Select(x => x.Name).Order().ToArray());
        await factory.User(Role.Trainer);
        var page = await client.GetFromJsonAsync<JsonElement>("/api/staff?page=1&pageSize=2");
        Assert.Equal(new[] { "items", "page", "pageSize", "total" }, page.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Equal(1, page.GetProperty("page").GetInt32());
        Assert.Equal(2, page.GetProperty("pageSize").GetInt32());
        var invalidPage = await client.GetAsync("/api/staff?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
        var error = await invalidPage.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "detail", "referenceId", "status", "title", "traceId", "type" }, error.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Equal(400, error.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("traceId").GetString()));
        var invalidLogin = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "missing@example.test", password = "invalid" });
        Assert.Equal(HttpStatusCode.Unauthorized, invalidLogin.StatusCode);
        Assert.Equal("invalid_credentials", (await invalidLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());
    }
}
