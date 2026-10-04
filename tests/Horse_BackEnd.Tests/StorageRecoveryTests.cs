using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class StorageRecoveryTests
{
    internal static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0];

    [Fact]
    public async Task ConfiguredCertificateProtectionWorksAcrossFreshHostsAndRequiredModeRejectsPlaintext()
    {
        var root = NewTemporaryRoot(); using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=HRCMS Config Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        var pfx = Path.Combine(root, "test.pfx"); await File.WriteAllBytesAsync(pfx, certificate.Export(X509ContentType.Pfx, "test-only"));
        WebApplication Host(bool plaintext = false)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development", ContentRootPath = root });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProtection:Path"] = Path.Combine(root, "host-keys"), ["DataProtection:KeyEncryption"] = plaintext ? "None" : "Certificate",
                ["DataProtection:RequireEncryptedKeys"] = "true", ["DataProtection:CertificatePath"] = pfx, ["DataProtection:CertificatePassword"] = "test-only"
            });
            builder.AddClubKeyProtection(); return builder.Build();
        }
        string secret;
        await using (var original = Host()) secret = original.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("recovery-test").Protect("protected-value");
        Assert.Contains("encryptedSecret", await File.ReadAllTextAsync(Assert.Single(Directory.GetFiles(Path.Combine(root, "host-keys"), "key-*.xml"))));
        await using (var restored = Host()) Assert.Equal("protected-value", restored.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("recovery-test").Unprotect(secret));
        await using var rejected = Host(true);
        Assert.Throws<InvalidOperationException>(() => rejected.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("recovery-test").Protect("value"));
        File.Delete(pfx);
    }

    [Fact]
    public async Task UploadedFileIsDownloadableScopedAndMissingFileReturns404()
    {
        await using var factory = new ClubFactory(); var owner = await factory.User(Role.HorseOwner); using var client = await factory.Client(owner);
        var registration = await Registration(factory, owner);
        using var form = Upload(); var response = await client.PostAsync($"/api/registrations/{registration.Id}/attachments", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var route = $"/api/registrations/{registration.Id}/attachments/{id}";
        var download = await client.GetAsync(route);
        Assert.Equal(Png, await download.Content.ReadAsByteArrayAsync());
        Assert.Equal("image/png", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal("nosniff", Assert.Single(download.Headers.GetValues("X-Content-Type-Options")));
        using var stranger = await factory.Client(await factory.User(Role.HorseOwner));
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync(route)).StatusCode);
        await WorkerTests.Read(factory, async db =>
        {
            var item = (await db.Attachments.FindAsync(id))!;
            using var scope = factory.Services.CreateScope();
            File.Delete(scope.ServiceProvider.GetRequiredService<UploadStorage>().Resolve(item.StorageName));
        });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route)).StatusCode);
    }

    [Fact]
    public async Task TamperedStorageNameCannotReadOutsideUploads()
    {
        await using var factory = new ClubFactory(); var owner = await factory.User(Role.HorseOwner); using var client = await factory.Client(owner);
        var registration = await Registration(factory, owner);
        var attachment = new Attachment { RegistrationId = registration.Id, UploadedBy = owner.Id, StorageName = "../keys/key.xml", FileName = "key.xml", ContentType = "image/png" };
        await WorkerTests.Read(factory, async db => { db.Attachments.Add(attachment); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/registrations/{registration.Id}/attachments/{attachment.Id}")).StatusCode);
    }

    [Fact]
    public async Task UncommittedPartialUploadIsRemovedButExistingFileIsNeverOverwritten()
    {
        await using var factory = new ClubFactory(); using var scope = factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<UploadStorage>();
        var name = Guid.NewGuid().ToString("N");
        await storage.Write(name, Png, CancellationToken.None);
        await Assert.ThrowsAsync<IOException>(() => storage.Write(name, new byte[] { 1 }, CancellationToken.None));
        Assert.Equal(Png, await File.ReadAllBytesAsync(storage.Resolve(name)));
        await storage.CleanupUncommitted(); Assert.False(File.Exists(storage.Resolve(name)));
        var cancelled = Guid.NewGuid().ToString("N"); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.Write(cancelled, Png, cancel.Token));
        await storage.CleanupUncommitted(); Assert.False(File.Exists(storage.Resolve(cancelled)));
    }

    [Fact]
    public async Task RecoveryBundleDetectsCorruptionAndRejectsExistingRestoreDestination()
    {
        var root = NewTemporaryRoot(); var database = Path.Combine(root, "source.db"); var uploads = Path.Combine(root, "uploads"); var keys = Path.Combine(root, "keys");
        Directory.CreateDirectory(uploads); Directory.CreateDirectory(keys); await File.WriteAllTextAsync(Path.Combine(keys, "sample.xml"), "test-key");
        using (var connection = new SqliteConnection($"Data Source={database};Pooling=False"))
        { connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "CREATE TABLE Example(Id INTEGER PRIMARY KEY); INSERT INTO Example VALUES(1);"; command.ExecuteNonQuery(); }
        var bundle = Path.Combine(root, "bundle"); SqliteRecoveryBundle.Create(database, uploads, keys, bundle);
        var destination = Path.Combine(root, "restored"); SqliteRecoveryBundle.Restore(bundle, destination);
        Assert.Throws<IOException>(() => SqliteRecoveryBundle.Restore(bundle, destination));
        await File.AppendAllTextAsync(Path.Combine(bundle, "keys/sample.xml"), "corruption");
        Assert.Throws<IOException>(() => SqliteRecoveryBundle.Verify(bundle));
        Assert.Throws<IOException>(() => SqliteRecoveryBundle.Restore(bundle, Path.Combine(root, "rejected")));
        Assert.False(Directory.Exists(Path.Combine(root, "rejected")));
    }

    [Fact]
    public async Task CertificateEncryptsPersistedKeysAndFreshProviderCanDecryptWithBackupKeyRing()
    {
        var root = NewTemporaryRoot(); using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=HRCMS Recovery Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        var sourceKeys = Path.Combine(root, "keys"); Directory.CreateDirectory(sourceKeys);
        var provider = DataProtectionProvider.Create(new DirectoryInfo(sourceKeys), options => options.SetApplicationName("HorseClub").ProtectKeysWithCertificate(certificate));
        var secret = provider.CreateProtector("HorseClub.PersonalData.NationalId").Protect("000000000001");
        var xml = await File.ReadAllTextAsync(Assert.Single(Directory.GetFiles(sourceKeys, "key-*.xml")));
        Assert.Contains("encryptedSecret", xml); Assert.DoesNotContain("<masterKey", xml);
        var backupKeys = Path.Combine(root, "restored-keys"); Directory.CreateDirectory(backupKeys);
        foreach (var file in Directory.GetFiles(sourceKeys)) File.Copy(file, Path.Combine(backupKeys, Path.GetFileName(file)));
        var restored = DataProtectionProvider.Create(new DirectoryInfo(backupKeys), options => options.SetApplicationName("HorseClub").ProtectKeysWithCertificate(certificate));
        Assert.Equal("000000000001", restored.CreateProtector("HorseClub.PersonalData.NationalId").Unprotect(secret));
        var withoutCertificate = DataProtectionProvider.Create(new DirectoryInfo(backupKeys), options => options.SetApplicationName("HorseClub"));
        Assert.ThrowsAny<CryptographicException>(() => withoutCertificate.CreateProtector("HorseClub.PersonalData.NationalId").Unprotect(secret));
    }

    [SqliteFact]
    public async Task SQLiteBundleRestoresApiLoginUploadedBytesAndProtectedPersonalData()
    {
        // This drill is provider-specific; SQL Server has its own native backup/restore process.
        var factory = new ClubFactory(); var root = NewTemporaryRoot();
        var account = await ClubFactory.Post(factory.CreateClient(), "/api/auth/register", new RegisterRequest("restore@example.test", "restoreowner", "Restore", "Owner", "0900", "Here", ClubFactory.Password, ClubFactory.Password, "000000000001"));
        var ownerId = account.GetProperty("id").GetGuid();
        User? owner = null; string protectedId = "";
        await WorkerTests.Read(factory, async db => { owner = (await db.Users.FindAsync(ownerId))!; owner.EmailVerified = true; protectedId = owner.NationalIdProtected!; await db.SaveChangesAsync(); });
        using var client = await factory.Client(owner); var registration = await Registration(factory, owner!);
        using var form = Upload(); var attachment = await client.PostAsync($"/api/registrations/{registration.Id}/attachments", form);
        var attachmentId = (await attachment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var config = factory.Services.GetRequiredService<IConfiguration>();
        var database = new SqliteConnectionStringBuilder(config.GetConnectionString("Sqlite")).DataSource;
        var uploads = config["Storage:Path"]!; var keys = config["DataProtection:Path"]!;
        await factory.DisposeAsync(); // Stop the host before capturing DB + uploads + keys.
        var bundle = Path.Combine(root, "bundle"); SqliteRecoveryBundle.Create(database, uploads, keys, bundle);
        var destination = Path.Combine(root, "restore"); SqliteRecoveryBundle.Restore(bundle, destination);
        await using var restored = new ClubFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Sqlite"] = $"Data Source={Path.Combine(destination, "database.db")}",
            ["Storage:Path"] = Path.Combine(destination, "uploads"), ["DataProtection:Path"] = Path.Combine(destination, "keys")
        });
        using var restoredClient = await restored.Client(owner);
        var me = await restoredClient.GetFromJsonAsync<JsonElement>("/api/auth/me"); Assert.Equal(ownerId, me.GetProperty("id").GetGuid());
        Assert.Equal(Png, await restoredClient.GetByteArrayAsync($"/api/registrations/{registration.Id}/attachments/{attachmentId}"));
        var protector = restored.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("HorseClub.PersonalData.NationalId");
        Assert.Equal("000000000001", protector.Unprotect(protectedId));
    }

    internal static string NewTemporaryRoot() { var path = Path.Combine(Path.GetTempPath(), "horseclub-recovery-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; }
    internal static MultipartFormDataContent Upload() { var form = new MultipartFormDataContent(); form.Add(new StringContent("HorsePhoto"), "type"); form.Add(new ByteArrayContent(Png), "file", "photo.png"); return form; }
    internal static async Task<HorseRegistration> Registration(ClubFactory factory, User owner)
    {
        var registration = new HorseRegistration { OwnerId = owner.Id, Name = "Recovery", Status = RegistrationStatus.Draft };
        await WorkerTests.Read(factory, async db => { db.Registrations.Add(registration); await db.SaveChangesAsync(); }); return registration;
    }
}
