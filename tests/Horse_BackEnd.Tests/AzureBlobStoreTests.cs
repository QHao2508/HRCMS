using System.Net;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using HorseClub.BLL.Common;
using Microsoft.Extensions.Options;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class AzureBlobStoreTests
{
    [Fact]
    public async Task UploadIsConditionalAndDownloadStreamsBytesWithoutLocalFiles()
    {
        var bytes = new byte[] { 137, 80, 78, 71 };
        var requests = new List<string>();
        using var handler = new StorageHandler(async request =>
        {
            requests.Add(request.Method.Method);
            if (request.Method == HttpMethod.Put)
            {
                Assert.Equal("*", Assert.Single(request.Headers.GetValues("If-None-Match")));
                Assert.Equal(bytes, await request.Content!.ReadAsByteArrayAsync());
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            if (request.Method == HttpMethod.Get)
            {
                var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
                result.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"test\"");
                return result;
            }
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var storage = Create(handler);
        var name = Guid.NewGuid().ToString("N");
        await storage.Write(name, bytes, CancellationToken.None);
        await using var stream = await storage.OpenRead(name, CancellationToken.None);
        using var output = new MemoryStream();
        await stream.CopyToAsync(output);
        Assert.Equal(bytes, output.ToArray());
        await storage.Delete(name, CancellationToken.None);
        Assert.Equal(new[] { "PUT", "GET", "DELETE" }, requests);
    }

    [Fact]
    public async Task PhotoUploadPreservesImageContentTypeInAzureHeaders()
    {
        using var handler = new StorageHandler(request =>
        {
            Assert.Equal("image/png", Assert.Single(request.Headers.GetValues("x-ms-blob-content-type")));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created));
        });
        await Create(handler).Write(Guid.NewGuid().ToString("N"), new byte[] { 137, 80, 78, 71 }, CancellationToken.None, "image/png");
    }

    [Fact]
    public async Task MissingBlobReturns404ButMissingContainerRemainsAnInfrastructureError()
    {
        foreach (var code in new[] { "BlobNotFound", "ContainerNotFound" })
        {
            using var handler = new StorageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.NotFound);
                response.Headers.Add("x-ms-error-code", code);
                return Task.FromResult(response);
            });
            var storage = Create(handler);
            if (code == "BlobNotFound")
            {
                var error = await Assert.ThrowsAsync<ApiException>(() => storage.OpenRead(Guid.NewGuid().ToString("N"), CancellationToken.None));
                Assert.Equal(404, error.Status);
            }
            else await Assert.ThrowsAsync<Azure.RequestFailedException>(() => storage.OpenRead(Guid.NewGuid().ToString("N"), CancellationToken.None));
        }
    }

    [Fact]
    public void AzureConfigurationRejectsMissingOrInsecureServiceAndInvalidContainer()
    {
        Assert.False(new StorageOptions { Provider = "AzureBlob" }.IsValid());
        Assert.False(new StorageOptions { Provider = "AzureBlob", ServiceUri = "http://account.blob.core.windows.net", Container = "files" }.IsValid());
        Assert.False(new StorageOptions { Provider = "AzureBlob", ServiceUri = "https://account.blob.core.windows.net", Container = "Files" }.IsValid());
        Assert.True(new StorageOptions { Provider = "AzureBlob", ServiceUri = "https://account.blob.core.windows.net", Container = "hrcms-files" }.IsValid());
    }

    private static AzureBlobStore Create(HttpMessageHandler handler)
    {
        var options = new BlobClientOptions { Transport = new HttpClientTransport(new HttpClient(handler, disposeHandler: false)) };
        options.Retry.MaxRetries = 0;
        var client = new BlobContainerClient(new Uri("https://account.blob.core.windows.net/hrcms-files"),
            new StorageSharedKeyCredential("account", Convert.ToBase64String(new byte[32])), options);
        return new AzureBlobStore(client);
    }

    private sealed class StorageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => respond(request);
    }
}
