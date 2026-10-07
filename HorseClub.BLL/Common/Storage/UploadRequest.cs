namespace HorseClub.BLL.Common;

/// <summary>Upload input without a dependency on an ASP.NET request.</summary>
public sealed record UploadRequest(bool HasFormContentType, Func<Task<UploadForm>> ReadForm, CancellationToken CancellationToken)
{
    /// <summary>
    /// Đọc multipart qua adapter đã được API cung cấp với cancellation token, không đưa HttpRequest vào BLL.
    /// </summary>
    public Task<UploadForm> ReadFormAsync() => ReadForm();
}
