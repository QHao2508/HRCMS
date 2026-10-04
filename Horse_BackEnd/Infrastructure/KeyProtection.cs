using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace Horse_BackEnd.Infrastructure;

public static class KeyProtection
{
    public static void AddClubKeyProtection(this WebApplicationBuilder builder)
    {
        builder.Services.AddDataProtection().SetApplicationName("HorseClub");
        builder.Services.AddSingleton<KeyMaterial>();
        builder.Services.AddOptions<KeyManagementOptions>().Configure<KeyMaterial, ILoggerFactory>((options, material, logs) =>
        {
            options.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(material.Path), logs);
            if (material.Certificate is not null) options.XmlEncryptor = new ConfiguredCertificateEncryptor(material.Certificate, logs);
            else if (material.Mode == "WindowsDpapi" && OperatingSystem.IsWindows()) options.XmlEncryptor = new DpapiXmlEncryptor(false, logs);
        }).ValidateOnStart();
    }

    private sealed class ConfiguredCertificateEncryptor(X509Certificate2 certificate, ILoggerFactory logs) : IXmlEncryptor
    {
        public EncryptedXmlInfo Encrypt(XElement plaintextElement)
        {
            var result = new CertificateXmlEncryptor(certificate, logs).Encrypt(plaintextElement);
            return new EncryptedXmlInfo(result.EncryptedElement, typeof(ConfiguredCertificateDecryptor));
        }
    }

    // Public constructor is required by the Data Protection key deserializer.
    public sealed class ConfiguredCertificateDecryptor(IServiceProvider services) : IXmlDecryptor
    {
        public XElement Decrypt(XElement encryptedElement)
        {
            var certificate = services.GetRequiredService<KeyMaterial>().Certificate
                ?? throw new InvalidOperationException("The original key encryption certificate is required for restore.");
            // Use the framework's certificate decryption setup with the final host certificate.
            // This avoids reading certificate configuration before host overrides are applied.
            var decryptionServices = new ServiceCollection();
            decryptionServices.AddDataProtection().UnprotectKeysWithAnyCertificate(certificate);
            using var decryptionProvider = decryptionServices.BuildServiceProvider();
            return new EncryptedXmlDecryptor(decryptionProvider).Decrypt(encryptedElement);
        }
    }

    private sealed class KeyMaterial : IDisposable
    {
        public string Path { get; }
        public string Mode { get; }
        public X509Certificate2? Certificate { get; }
        public KeyMaterial(IConfiguration config, IWebHostEnvironment environment)
        {
            var section = config.GetSection("DataProtection");
            Path = System.IO.Path.GetFullPath(section["Path"] ?? System.IO.Path.Combine(environment.ContentRootPath, "App_Data", "keys"));
            Directory.CreateDirectory(Path);
            var mode = section["KeyEncryption"] ?? "None";
            Mode = mode;
            switch (mode)
            {
                case "None":
                    if (section.GetValue("RequireEncryptedKeys", !environment.IsDevelopment()))
                        throw new InvalidOperationException("Configure DataProtection:KeyEncryption=Certificate or WindowsDpapi before starting outside Development.");
                    break;
                case "WindowsDpapi":
                    if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("WindowsDpapi key encryption requires Windows.");
                    break;
                case "Certificate":
                    var certificatePath = section["CertificatePath"] ?? throw new InvalidOperationException("Configure DataProtection:CertificatePath.");
                    var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, section["CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet);
                    if (!certificate.HasPrivateKey) { certificate.Dispose(); throw new InvalidOperationException("Data Protection certificate requires a private key."); }
                    Certificate = certificate;
                    break;
                default: throw new InvalidOperationException("DataProtection:KeyEncryption must be None, Certificate, or WindowsDpapi.");
            }
        }
        public void Dispose() => Certificate?.Dispose();
    }
}
