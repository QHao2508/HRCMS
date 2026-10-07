namespace HorseClub.BLL.Common;

/// <summary>Application outcome. The API layer turns it into an HTTP response.</summary>
public sealed record OperationResult(int StatusCode, object? Value = null, string? Location = null, FileDownload? Download = null)
{
    /// <summary>
    /// Tạo OperationResult Ok thuần dữ liệu để layer API quyết định JSON/status hoặc stream HTTP.
    /// </summary>
    /// <param name="value">Giá trị kiểu object? dùng trong Ok.</param>
    public static OperationResult Ok(object? value) => new(200, value);
    /// <summary>
    /// Tạo OperationResult Created thuần dữ liệu để layer API quyết định JSON/status hoặc stream HTTP.
    /// </summary>
    /// <param name="location">Giá trị kiểu string dùng trong Created.</param>
    /// <param name="value">Giá trị kiểu object? dùng trong Created.</param>
    public static OperationResult Created(string location, object? value) => new(201, value, location);
    /// <summary>
    /// Tạo OperationResult NoContent thuần dữ liệu để layer API quyết định JSON/status hoặc stream HTTP.
    /// </summary>
    public static OperationResult NoContent() => new(204);
    /// <summary>
    /// Tạo OperationResult File thuần dữ liệu để layer API quyết định JSON/status hoặc stream HTTP.
    /// </summary>
    /// <param name="content">Giá trị kiểu Stream dùng trong File.</param>
    /// <param name="contentType">MIME đã xác định từ nội dung file; dùng khi lưu/stream để browser đọc đúng.</param>
    /// <param name="fileName">Giá trị kiểu string dùng trong File.</param>
    public static OperationResult File(Stream content, string contentType, string fileName) => new(200, Download: new(content, contentType, fileName));
}
