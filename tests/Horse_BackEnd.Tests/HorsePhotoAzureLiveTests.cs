using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class AzurePhotoFactAttribute : FactAttribute
{
    public AzurePhotoFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRCMS_AZURE_PHOTO_SMOKE") != "1")
            Skip = "Explicitly enable HRCMS_AZURE_PHOTO_SMOKE for a temporary image upload to the configured private Azure container.";
    }
}

public sealed class HorsePhotoAzureLiveTests
{
    [AzurePhotoFact]
    public async Task ActualUploadUsesPrivateAzureBlobAndSqlMetadataWithoutLocalImageFiles()
    {
        var secretPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "horseclub-backend", "secrets.json");
        var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(secretPath))!;
        string? Setting(string name) => secrets.GetValueOrDefault("Storage:" + name);
        Assert.Equal("AzureBlob", Setting("Provider")); Assert.Equal("hrcms-files", Setting("Container"));
        Assert.Equal("https://hrcmsg2.blob.core.windows.net/", Setting("ServiceUri"));
        var localPath = Path.Combine(Path.GetTempPath(), "hrcms-azure-photo-" + Guid.NewGuid().ToString("N"));
        var settings = new Dictionary<string, string?> { ["Storage:Path"] = localPath };
        foreach (var name in new[] { "Provider", "ServiceUri", "Container", "ConnectionString", "IdentityCredential" }) settings["Storage:" + name] = Setting(name);
        var container = !string.IsNullOrWhiteSpace(Setting("ConnectionString"))
            ? new BlobServiceClient(Setting("ConnectionString")).GetBlobContainerClient(Setting("Container"))
            : new BlobServiceClient(new Uri(Setting("ServiceUri")!), new AzureCliCredential()).GetBlobContainerClient(Setting("Container"));
        await using var factory = new ClubFactory(settings); var owner = await factory.User(Role.HorseOwner); using var client = await factory.Client(owner);
        var bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl6L1sAAAAASUVORK5CYII=");
        var draft = await ClubFactory.Post(client, "/api/registrations", new { name = "Azure photo smoke test" }); var id = draft.GetProperty("id").GetGuid();
        string? storageName = null;
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("HorsePhoto"), "type"); var image = new ByteArrayContent(bytes); image.Headers.ContentType = new("image/png"); form.Add(image, "file", "horse.png");
            var response = await client.PostAsync($"/api/registrations/{id}/attachments", form);
            await WorkerTests.Read(factory, async db => storageName = await db.Attachments.Where(a => a.RegistrationId == id).Select(a => a.StorageName).SingleOrDefaultAsync());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode); Assert.NotNull(storageName);
            var blob = container.GetBlobClient(storageName);
            Assert.Equal("image/png", (await blob.GetPropertiesAsync()).Value.ContentType);
            Assert.Equal(Azure.Storage.Blobs.Models.PublicAccessType.None, (await container.GetAccessPolicyAsync()).Value.BlobPublicAccess);
            Assert.Equal(bytes, (await blob.DownloadContentAsync()).Value.Content.ToArray());
            var downloaded = await client.GetByteArrayAsync($"/api/registrations/{id}/attachments/" + (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
            Assert.Equal(bytes, downloaded); Assert.False(Directory.Exists(localPath));
            using var scope = factory.Services.CreateScope(); Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<UploadStorage>().Root);
        }
        finally
        {
            if (storageName is not null) await container.GetBlobClient(storageName).DeleteIfExistsAsync();
        }
    }
}
