using System.Text.RegularExpressions;
using HorseClub.DAL.Data;
using HorseClub.BLL.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Common;

public sealed class UploadStorage(IOptions<StorageOptions> options, IHostEnvironment environment, ClubDbContext db, ILogger<UploadStorage> logger, AzureBlobStore blobs)
{
    private readonly List<string> created = [];
    private bool committed;
    private bool UsesAzure => options.Value.Provider == "AzureBlob";
    public string Root => !UsesAzure ? Path.GetFullPath(options.Value.Path ?? Path.Combine(environment.ContentRootPath, "App_Data", "uploads"))
        : throw new InvalidOperationException("Azure uploads do not have a local directory.");

    /// <summary>
    /// Chỉ cho phép storage key UUID dạng 32 ký tự hex để ngăn path traversal ở luồng upload hồ sơ.
    /// </summary>
    /// <param name="name">Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm.</param>
    private static void ValidateName(string name) => Ensure.That(Regex.IsMatch(name, "^[a-f0-9]{32}$"), Messages.Get(MessageKey.StoredAttachmentIsUnavailable), 404, "not_found");

    /// <summary>
    /// Ghép đường dẫn local an toàn cho provider kiểm thử, kiểm tra symlink/reparse point; Azure không có thư mục local.
    /// </summary>
    /// <param name="storageName">Blob/file key đã kiểm tra, không phải URL public hoặc đường dẫn do client tùy ý chọn.</param>
    public string Resolve(string storageName)
    {
        ValidateName(storageName);
        Directory.CreateDirectory(Root);
        for (var directory = Root; !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
            Ensure.That((File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0, Messages.Get(MessageKey.StoredAttachmentIsUnavailable), 404, "not_found");
        var path = Path.Combine(Root, storageName);
        if (File.Exists(path))
            Ensure.That((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, Messages.Get(MessageKey.StoredAttachmentIsUnavailable), 404, "not_found");
        return path;
    }

    /// <summary>
    /// Ghi file mới theo provider, theo dõi blob/file vừa tạo để có thể dọn khi transaction rollback; không ghi đè key có sẵn.
    /// </summary>
    /// <param name="storageName">Blob/file key đã kiểm tra, không phải URL public hoặc đường dẫn do client tùy ý chọn.</param>
    /// <param name="bytes">Nội dung file trong bộ nhớ, đã được caller kiểm loại và giới hạn dung lượng.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    /// <param name="contentType">MIME đã xác định từ nội dung file; dùng khi lưu/stream để browser đọc đúng.</param>
    /// <remarks>Có ghi blob/file; tài nguyên chưa commit được cơ chế upload đối soát/dọn.</remarks>
    public async Task Write(string storageName, byte[] bytes, CancellationToken token, string contentType = "application/octet-stream")
    {
        ValidateName(storageName);
        if (UsesAzure)
        {
            // Track before the call: a successful Azure write may lose its acknowledgement.
            // IDs are random and writes are conditional; a collision must not be deleted.
            try { await blobs.Write(storageName, bytes, token, contentType); created.Add(storageName); }
            catch (Azure.RequestFailedException error) when (error.Status is 409 or 412) { throw; }
            catch { created.Add(storageName); throw; }
            return;
        }
        var path = Resolve(storageName);
        // CreateNew prevents overwriting an existing file, even on an accidental ID collision.
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        created.Add(storageName);
        await stream.WriteAsync(bytes, token);
        await stream.FlushAsync(token);
    }

    /// <summary>
    /// Đánh dấu upload đã được commit với database để cơ chế dọn cuối request không xóa tài nguyên hợp lệ.
    /// </summary>
    public void Commit() => committed = true;

    /// <summary>
    /// Đọc blob Azure hoặc file kiểm thử sau khi kiểm storage key; trả stream để không tải cả file vào response buffer.
    /// </summary>
    /// <param name="storageName">Blob/file key đã kiểm tra, không phải URL public hoặc đường dẫn do client tùy ý chọn.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    public async Task<Stream> OpenRead(string storageName, CancellationToken token = default)
    {
        ValidateName(storageName);
        if (UsesAzure) return await blobs.OpenRead(storageName, token);
        var path = Resolve(storageName);
        Ensure.That(File.Exists(path), Messages.Get(MessageKey.StoredAttachmentIsUnavailable), 404, "not_found");
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
    }

    /// <summary>
    /// Dọn tài nguyên mới không được commit, nhưng giữ tài nguyên DB đang tham chiếu hoặc chưa xác nhận được trạng thái DB; log ID để đối soát.
    /// </summary>
    public async Task CleanupUncommitted()
    {
        if (committed) return;
        foreach (var name in created)
        {
            try
            {
                // A commit acknowledgement can be lost. Keep any file referenced by the DB.
                // If the database is unavailable, preserve the file for later reconciliation.
                if (await db.Attachments.AnyAsync(x => x.StorageName == name) || await db.IncidentPhotos.AnyAsync(x => x.StorageName == name)) continue;
                  if (UsesAzure)
                  {
                      using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                      await blobs.Delete(name, timeout.Token);
                  }
                  else File.Delete(Resolve(name));
            }
            catch (Exception error)
            {
                logger.LogWarning("Uncommitted upload {StorageName} requires reconciliation: {ExceptionType}", name, error.GetType().Name);
            }
        }
    }
}
