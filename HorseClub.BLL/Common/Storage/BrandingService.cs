using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Common;

/// <summary>Only the configured branding image is public; no request can choose another private blob.</summary>
public sealed class BrandingService(AzureBlobStore blobs, IOptions<BrandingOptions> options)
{
    /// <summary>
    /// Chỉ đọc blob logo cố định từ BrandingOptions; người gọi không thể chọn một blob ảnh ngựa khác.
    /// </summary>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    public Task<Stream> OpenLogo(CancellationToken token) => blobs.OpenRead(options.Value.LogoBlobName, token);
}
