using System.Text.RegularExpressions;
using Horse_BackEnd.Data;
using HorseClub.BLL.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Horse_BackEnd.Infrastructure;

public sealed class UploadStorage(IOptions<StorageOptions> options, IWebHostEnvironment environment, ClubDbContext db, ILogger<UploadStorage> logger)
{
    private readonly List<string> created = [];
    private bool committed;
    public string Root => Path.GetFullPath(options.Value.Path ?? Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));

    public string Resolve(string storageName)
    {
        Ensure.That(Regex.IsMatch(storageName, "^[a-f0-9]{32}$"), Messages.Get(MessageKey.StoredAttachmentIsUnavailable), 404, "not_found");
        Directory.CreateDirectory(Root);
        for (var directory = Root; !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
            Ensure.That((File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0, Messages.Get(MessageKey.StoredAttachmentIsUnavailable), 404, "not_found");
        var path = Path.Combine(Root, storageName);
        if (File.Exists(path))
            Ensure.That((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, Messages.Get(MessageKey.StoredAttachmentIsUnavailable), 404, "not_found");
        return path;
    }

    public async Task Write(string storageName, byte[] bytes, CancellationToken token)
    {
        var path = Resolve(storageName);
        // CreateNew prevents overwriting an existing file, even on an accidental ID collision.
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        created.Add(storageName);
        await stream.WriteAsync(bytes, token);
        await stream.FlushAsync(token);
    }

    public void Commit() => committed = true;

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
                File.Delete(Resolve(name));
            }
            catch (Exception error)
            {
                logger.LogWarning("Uncommitted upload {StorageName} requires reconciliation: {ExceptionType}", name, error.GetType().Name);
            }
        }
    }
}
