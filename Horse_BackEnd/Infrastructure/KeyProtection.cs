using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

namespace Horse_BackEnd.Infrastructure;

public static class KeyProtection
{
    /// <summary>
    /// Đăng ký kho Data Protection key và cơ chế mã hóa theo môi trường; giữ key ổn định giữa các lần chạy để token còn giải mã được.
    /// </summary>
    /// <param name="builder">Giá trị kiểu WebApplicationBuilder dùng trong AddClubKeyProtection.</param>
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
        /// <summary>
        /// Mã hóa XML Data Protection bằng certificate đã cấu hình và chỉ định decryptor dùng khi khôi phục key.
        /// </summary>
        /// <param name="plaintextElement">Giá trị kiểu XElement dùng trong Encrypt.</param>
        public EncryptedXmlInfo Encrypt(XElement plaintextElement)
        {
            var result = new CertificateXmlEncryptor(certificate, logs).Encrypt(plaintextElement);
            return new EncryptedXmlInfo(result.EncryptedElement, typeof(ConfiguredCertificateDecryptor));
        }
    }

    // Public constructor is required by the Data Protection key deserializer.
    public sealed class ConfiguredCertificateDecryptor(IServiceProvider services) : IXmlDecryptor
    {
        /// <summary>
        /// Giải mã XML key bằng certificate gốc từ DI; không thể khôi phục token nếu thiếu private key tương ứng.
        /// </summary>
        /// <param name="encryptedElement">Giá trị kiểu XElement dùng trong Decrypt.</param>
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
        /// <summary>
        /// Đọc cấu hình đường dẫn/key encryption, kiểm môi trường và certificate private key; tạo kho key cần được giữ khi dọn build.
        /// </summary>
        /// <param name="config">Giá trị kiểu IConfiguration dùng trong KeyMaterial.</param>
        /// <param name="environment">Giá trị kiểu IWebHostEnvironment dùng trong KeyMaterial.</param>
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
        /// <summary>
        /// Giải phóng certificate/nguồn lực mật mã khi DI scope/container kết thúc.
        /// </summary>
        public void Dispose() => Certificate?.Dispose();
    }
}
