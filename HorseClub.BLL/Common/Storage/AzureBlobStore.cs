using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Common;

/// <summary>Private blob storage. Access to downloads stays behind the authorized API.</summary>
public sealed class AzureBlobStore
{
    private readonly BlobContainerClient? container;

    /// <summary>
    /// Tạo adapter BlobContainerClient từ credential cấu hình hoặc client được inject trong kiểm thử; không xuất khóa ra frontend.
    /// </summary>
    /// <param name="container">Giá trị kiểu BlobContainerClient dùng trong AzureBlobStore.</param>
    public AzureBlobStore(BlobContainerClient container) => this.container = container;

    /// <summary>
    /// Tạo adapter BlobContainerClient từ credential cấu hình hoặc client được inject trong kiểm thử; không xuất khóa ra frontend.
    /// </summary>
    /// <param name="options">Cấu hình/hợp đồng tùy hàm; các giá trị được truyền rõ ràng từ caller.</param>
    public AzureBlobStore(IOptions<StorageOptions> options)
    {
        var settings = options.Value;
        if (settings.Provider != "AzureBlob") return;
        container = string.IsNullOrWhiteSpace(settings.ConnectionString)
            ? new BlobServiceClient(new Uri(settings.ServiceUri!), settings.IdentityCredential == "AzureCli"
                ? new AzureCliCredential(new AzureCliCredentialOptions { ProcessTimeout = TimeSpan.FromSeconds(15) })
                : new DefaultAzureCredential()).GetBlobContainerClient(settings.Container)
            : new BlobServiceClient(settings.ConnectionString).GetBlobContainerClient(settings.Container);
    }

    /// <summary>
    /// Upload blob với điều kiện If-None-Match để không ghi đè, giữ MIME của nội dung.
    /// </summary>
    /// <param name="name">Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm.</param>
    /// <param name="bytes">Nội dung file trong bộ nhớ, đã được caller kiểm loại và giới hạn dung lượng.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    /// <param name="contentType">MIME đã xác định từ nội dung file; dùng khi lưu/stream để browser đọc đúng.</param>
    public async Task Write(string name, byte[] bytes, CancellationToken token, string contentType = "application/octet-stream")
    {
        // Never overwrite another upload. Container provisioning is an explicit setup step.
        await Required().GetBlobClient(name).UploadAsync(new BinaryData(bytes), new BlobUploadOptions
        {
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, token);
    }

    /// <summary>
    /// Mở stream download từ container private; chỉ chuyển BlobNotFound thành 404, giữ lỗi hạ tầng để xử lý đúng.
    /// </summary>
    /// <param name="name">Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    public async Task<Stream> OpenRead(string name, CancellationToken token)
    {
        try { return (await Required().GetBlobClient(name).DownloadStreamingAsync(cancellationToken: token)).Value.Content; }
        catch (RequestFailedException error) when (error.Status == 404 && error.ErrorCode == "BlobNotFound")
        { throw new ApiException(404, "not_found", "Stored attachment is unavailable."); }
    }

    /// <summary>
    /// Xóa blob cùng snapshot nếu tồn tại; dùng trong dọn upload chưa commit hoặc kiểm thử.
    /// </summary>
    /// <param name="name">Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    public async Task Delete(string name, CancellationToken token) =>
        await Required().GetBlobClient(name).DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: token);

    /// <summary>
    /// Yêu cầu container Azure đã được cấu hình trước khi thao tác; không tự fallback sang local.
    /// </summary>
    private BlobContainerClient Required() => container ?? throw new InvalidOperationException("Azure Blob Storage is not configured.");
}
