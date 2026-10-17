namespace Horse_BackEnd.Infrastructure;

public static class UploadRequestAdapter
{
    /// <summary>
    /// Chuyển HttpRequest/form/files thành abstraction UploadRequest của BLL, giữ HTTP phụ thuộc ở layer API.
    /// </summary>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    public static UploadRequest Create(HttpRequest request) => new(request.HasFormContentType, async () =>
    {
        var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
        return new UploadForm(form.ToDictionary(x => x.Key, x => x.Value.ToString()),
            new UploadFiles(form.Files.Select(x => new UploadFile(x.Name, x.FileName, x.Length, x.OpenReadStream)).ToArray()));
    }, request.HttpContext.RequestAborted);
}
