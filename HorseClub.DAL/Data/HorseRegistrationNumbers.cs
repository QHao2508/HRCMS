namespace HorseClub.DAL.Data;

/// <summary>
/// Mã đăng ký ngựa do câu lạc bộ cấp khi Quản lý duyệt hồ sơ: "HRC-" + số thứ tự 6 chữ số (HRC-000001).
/// Số lấy từ SQL Server SEQUENCE nên hai lần duyệt cùng lúc không trùng; cột có unique index để chặn trùng tuyệt đối.
/// </summary>
public static class HorseRegistrationNumbers
{
    public const string Sequence = "HorseRegistrationNumbers";
    public const int MaxLength = 100;
    public static string Format(long number) => "HRC-" + number.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
}
