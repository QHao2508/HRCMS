using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class DeploymentKeyProtectionTests
{
    [Theory]
    [InlineData("Development", false)]
    [InlineData("Production", true)]
    public void CertificateIsRequiredOnlyOutsideDevelopment(string environment, bool requiresCertificate)
    {
        var directory = Path.Combine(Path.GetTempPath(), "hrcms-dev-keys-" + Guid.NewGuid());
        try
        {
            using var services = CreateServices(new Dictionary<string, string?>
            {
                ["DataProtection:Path"] = directory
            }, environment);
            if (requiresCertificate)
            {
                Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<IDataProtectionProvider>());
            }
            else
            {
                var protector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("development-test");
                Assert.Equal("local-data", protector.Unprotect(protector.Protect("local-data")));
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CertificateKeysCanBeRestoredAfterRestart(bool useBase64)
    {
        var directory = Path.Combine(Path.GetTempPath(), "hrcms-keys-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=HRCMS deployment test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            var password = Guid.NewGuid().ToString();
            var pfx = certificate.Export(X509ContentType.Pfx, password);
            var certificatePath = Path.Combine(directory, "certificate.pfx");
            File.WriteAllBytes(certificatePath, pfx);
            var settings = new Dictionary<string, string?>
            {
                ["DataProtection:Path"] = directory,
                ["DataProtection:KeyEncryption"] = "Certificate",
                ["DataProtection:CertificatePassword"] = password,
                [useBase64 ? "DataProtection:CertificateBase64" : "DataProtection:CertificatePath"] = useBase64 ? Convert.ToBase64String(pfx) : certificatePath
            };
            string protectedValue;
            using (var services = CreateServices(settings))
                protectedValue = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("deployment-test").Protect("private-data");
            using (var services = CreateServices(settings))
                Assert.Equal("private-data", services.GetRequiredService<IDataProtectionProvider>().CreateProtector("deployment-test").Unprotect(protectedValue));
            var keyFile = Assert.Single(Directory.GetFiles(directory, "key-*.xml"));
            Assert.Contains("encryptedSecret", File.ReadAllText(keyFile));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static ServiceProvider CreateServices(Dictionary<string, string?> settings, string environment = "Production")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.AddClubKeyProtection();
        return builder.Services.BuildServiceProvider();
    }
}
