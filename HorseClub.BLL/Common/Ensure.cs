using HorseClub.BLL.Messaging;
using System.ComponentModel.DataAnnotations;
using System.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Common;

public static class Ensure
{
    /// <summary>
    /// Chuyển điều kiện nghiệp vụ sai thành ApiException với HTTP status, mã lỗi và reference ID do caller chỉ định.
    /// </summary>
    /// <param name="condition">Giá trị kiểu bool dùng trong That.</param>
    /// <param name="message">Mã thông điệp + tham số; client dịch theo mã, không phụ thuộc câu chữ.</param>
    /// <param name="status">Trạng thái enum API, tách khỏi nhãn tiếng Việt.</param>
    /// <param name="code">Mã lỗi kỹ thuật ổn định (ví dụ validation_error, forbidden).</param>
    public static void That(bool condition, MessageText message, int status = 400, string code = "validation_error")
    { if (!condition) throw new ApiException(status, code, message); }
    /// <summary>
    /// Trả entity khi tồn tại hoặc ném lỗi not_found; giúp service không tiếp tục xử lý dữ liệu null.
    /// </summary>
    /// <param name="value">Giá trị kiểu T? dùng trong Found.</param>
    public static T Found<T>(T? value) where T : class => value ?? throw new ApiException(404, "not_found", Messages.Text(MessageKey.RecordNotFound));
    /// <summary>
    /// Yêu cầu user thuộc một trong các vai trò cho phép; chặn thao tác nghiệp vụ trái quyền.
    /// </summary>
    /// <param name="user">Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép.</param>
    /// <param name="roles">Giá trị kiểu Role[] dùng trong Role.</param>
    public static void Role(User user, params Role[] roles) => That(roles.Contains(user.Role), Messages.Text(MessageKey.PermissionDenied), 403, "forbidden");
    /// <summary>
    /// Thực hiện kiểm tra DataAnnotations cho đối tượng và tập hợp lỗi hợp đồng dữ liệu.
    /// </summary>
    /// <param name="value">Giá trị kiểu object dùng trong Validate.</param>
    public static void Validate(object value)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(value, new ValidationContext(value), errors, true))
            throw new ApiException(400, "validation_error", Messages.Text(MessageKey.Invalid, string.Join(", ", errors.SelectMany(x => x.MemberNames).Distinct())));
        foreach (var property in value.GetType().GetProperties())
        {
            var p = property.GetValue(value);
            if (p is string s) That(s.Length <= 4000, Messages.Text(MessageKey.Exceeds4000Characters, property.Name));
            if (p is Enum e) That(Enum.IsDefined(e.GetType(), e), Messages.Text(MessageKey.Invalid, property.Name));
            if (p?.GetType().Namespace == "HorseClub.BLL.Contracts") Validate(p);
        }
    }
}
