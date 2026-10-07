using System.Net;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class BrandingTests
{
    [Fact]
    public async Task AnonymousLogoOnlyReadsConfiguredBlobAndLeavesPrivateDownloadsProtected()
    {
        var bytes = new byte[] {137,80,78,71,13,10,26,10}; var paths = new List<string>();
        using var handler = new LogoHandler(request =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            result.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"logo\"");
            return result;
        });
        var clientOptions = new BlobClientOptions { Transport = new HttpClientTransport(new HttpClient(handler, disposeHandler: false)) };
        clientOptions.Retry.MaxRetries = 0;
        var store = new AzureBlobStore(new BlobContainerClient(new Uri("https://account.blob.core.windows.net/hrcms-files"),
            new StorageSharedKeyCredential("account", Convert.ToBase64String(new byte[32])), clientOptions));
        await using var factory = new ClubFactory();
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => { services.RemoveAll<AzureBlobStore>(); services.AddSingleton(store); }));
        using var client = app.CreateClient();
        var response = await client.GetAsync("/api/branding/logo?blob=private-photo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync()); Assert.True(response.Headers.CacheControl!.Public);
        Assert.Equal(TimeSpan.FromDays(1), response.Headers.CacheControl.MaxAge);
        Assert.Equal("/hrcms-files/branding/hrcms-logo-153411c4096984a2.png", Assert.Single(paths));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/branding/private-photo")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/registrations/{Guid.NewGuid()}/attachments")).StatusCode);
    }

    private sealed class LogoHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
