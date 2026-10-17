namespace HorseClub.BLL.Common;

public sealed record UploadFiles(IReadOnlyList<UploadFile> Items)
{
    /// <summary>
    /// Tìm tệp có tên form tương ứng trong abstraction danh sách upload; trả null khi không có.
    /// </summary>
    /// <param name="name">Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm.</param>
    public UploadFile? GetFile(string name) => Items.FirstOrDefault(x => x.Name == name);
}
