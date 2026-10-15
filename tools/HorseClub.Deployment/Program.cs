using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml.Linq;
using Horse_BackEnd.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.SqlClient;

if (args.Length == 2 && args[0] == "verify-azure")
{
    VerifyAzureData(Path.GetFullPath(args[1]));
    return;
}

if (args.Length != 2) throw new ArgumentException("Usage: HorseClub.Deployment <existing-plaintext-key-directory> <new-private-output-directory>");
var source = Path.GetFullPath(args[0]);
var destination = Path.GetFullPath(args[1]);
if (Directory.Exists(destination)) throw new InvalidOperationException("Output directory must not exist; existing artifacts are never overwritten.");
var sourceFiles = Directory.GetFiles(source, "*.xml");
if (!sourceFiles.Any(path => Path.GetFileName(path).StartsWith("key-", StringComparison.Ordinal)))
    throw new InvalidOperationException("No existing key ring found.");
var documents = sourceFiles.Select(path => (Name: Path.GetFileName(path), Document: XDocument.Load(path))).ToArray();
if (documents.Any(item => item.Document.Descendants().Any(element => element.Name.LocalName == "encryptedSecret")))
    throw new InvalidOperationException("Source contains encrypted keys. Restore using its original certificate or Windows identity; this tool only packages plaintext development keys.");
var originalProvider = DataProtectionProvider.Create(new DirectoryInfo(source), options => options.SetApplicationName("HorseClub").DisableAutomaticKeyGeneration());
var marker = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
var protectedMarker = originalProvider.CreateProtector("HorseClub.Deployment.Verification").Protect(marker);
using var rsa = RSA.Create(3072);
var request = new CertificateRequest("CN=HRCMS Data Protection", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(5));
var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
var pfx = certificate.Export(X509ContentType.Pfx, password);
Directory.CreateDirectory(destination);
var keysPath = Path.Combine(destination, "keys");
Directory.CreateDirectory(keysPath);
var encryptor = new CertificateXmlEncryptor(certificate, NullLoggerFactory.Instance);
foreach (var item in documents)
{
    var secrets = item.Document.Descendants().Where(element => element.Attributes().Any(attribute => attribute.Name.LocalName == "requiresEncryption" && attribute.Value == "true")).ToArray();
    foreach (var secret in secrets)
    {
        var encrypted = encryptor.Encrypt(new XElement(secret));
        secret.ReplaceWith(new XElement(XName.Get("encryptedSecret", "http://schemas.asp.net/2015/03/dataProtection"),
            new XAttribute("decryptorType", typeof(KeyProtection.ConfiguredCertificateDecryptor).AssemblyQualifiedName!), encrypted.EncryptedElement));
    }
    item.Document.Save(Path.Combine(keysPath, item.Name));
}
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
builder.Configuration.Sources.Clear();
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["DataProtection:Path"] = keysPath,
    ["DataProtection:KeyEncryption"] = "Certificate",
    ["DataProtection:CertificateBase64"] = Convert.ToBase64String(pfx),
    ["DataProtection:CertificatePassword"] = password
});
builder.AddClubKeyProtection();
builder.Services.AddDataProtection().DisableAutomaticKeyGeneration();
using (var services = builder.Services.BuildServiceProvider())
{
    var protector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("HorseClub.Deployment.Verification");
    if (protector.Unprotect(protectedMarker) != marker) throw new CryptographicException("Existing-key restore validation failed.");
    var roundTrip = protector.Protect(marker);
    if (originalProvider.CreateProtector("HorseClub.Deployment.Verification").Unprotect(roundTrip) != marker)
        throw new CryptographicException("Key identity validation failed.");
}
File.WriteAllBytes(Path.Combine(destination, "data-protection.pfx"), pfx);
File.WriteAllText(Path.Combine(destination, "certificate-settings.private.json"), JsonSerializer.Serialize(new Dictionary<string, string>
{
    ["DataProtection__CertificateBase64"] = Convert.ToBase64String(pfx),
    ["DataProtection__CertificatePassword"] = password
}, new JsonSerializerOptions { WriteIndented = true }));
CryptographicOperations.ZeroMemory(pfx);
Console.WriteLine("Encrypted key-ring copy prepared and restore verified. Original keys were not changed.");
Console.WriteLine("Private artifacts: " + destination);

static void VerifyAzureData(string directory)
{
    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(directory, "azure-appsettings.private.json")))!;
    var verificationBuilder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
    verificationBuilder.Configuration.Sources.Clear();
    verificationBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["DataProtection:Path"] = Path.Combine(directory, "keys"),
        ["DataProtection:KeyEncryption"] = "Certificate",
        ["DataProtection:CertificateBase64"] = settings["DataProtection__CertificateBase64"],
        ["DataProtection:CertificatePassword"] = settings["DataProtection__CertificatePassword"]
    });
    verificationBuilder.AddClubKeyProtection();
    verificationBuilder.Services.AddDataProtection().DisableAutomaticKeyGeneration();
    using var verificationServices = verificationBuilder.Services.BuildServiceProvider();
    var protector = verificationServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("HorseClub.PersonalData.NationalId");
    using var connection = new SqlConnection(settings["ConnectionStrings__SqlServer"]);
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT NationalIdProtected FROM dbo.Users WHERE NationalIdProtected IS NOT NULL AND NationalIdProtected <> '';";
    using var reader = command.ExecuteReader();
    var checkedValues = 0;
    var failures = 0;
    while (reader.Read())
    {
        try { protector.Unprotect(reader.GetString(0)); }
        catch (CryptographicException) { failures++; }
        checkedValues++;
    }
    Console.WriteLine($"Azure protected values checked: {checkedValues}; decryption failures: {failures}. No values were printed or modified.");
    if (failures > 0) throw new InvalidOperationException("Prepared keys cannot decrypt all existing Azure values. Obtain the original key ring before deployment.");
}
