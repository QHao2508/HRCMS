using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Common;

public sealed class StorageOptions
{
    public const string Section = "Storage";
    public string Provider { get; set; } = "Local";
    public string? ServiceUri { get; set; }
    public string? Container { get; set; }
    public string? ConnectionString { get; set; }
    public string IdentityCredential { get; set; } = "Default";
    public string? Path { get; set; }
    [Range(1, 104857600)] public long MaxFileBytes { get; set; } = 10485760;
    [Range(1, 100)] public int MaxAttachmentsPerRecord { get; set; } = 20;
    [Range(1, 1000)] public int RequestsPerMinute { get; set; } = 20;

    /// <summary>
    /// Kiểm provider, HTTPS, container và credential mode; từ chối cấu hình Azure thiếu hoặc không an toàn.
    /// </summary>
    public bool IsValid() => Provider == "Local" || Provider == "AzureBlob"
        && IdentityCredential is "Default" or "AzureCli"
        && !string.IsNullOrWhiteSpace(Container)
        && System.Text.RegularExpressions.Regex.IsMatch(Container, "^(?!.*--)[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$")
        && (string.IsNullOrWhiteSpace(ConnectionString)
            ? Uri.TryCreate(ServiceUri, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host.EndsWith(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase)
            : ConnectionString.Contains("DefaultEndpointsProtocol=https", StringComparison.OrdinalIgnoreCase));
}
