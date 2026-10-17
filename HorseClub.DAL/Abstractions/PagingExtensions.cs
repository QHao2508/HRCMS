using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Abstractions;

public static class PagingExtensions
{
    /// <summary>
    /// Đọc một trang; chỉ chạy thêm COUNT khi trang đầy. Trang cuối (ít hơn size dòng) đã biết tổng = số dòng bỏ qua + số dòng nhận,
    /// nên bớt được một lượt gọi Azure SQL (~50 ms) cho phần lớn danh sách.
    /// </summary>
    public static async Task<DataPage<T>> ToPageAsync<T>(this IQueryable<T> query, int page, int size)
    {
        var skip = (page - 1) * size;
        var items = await query.Skip(skip).Take(size).ToListAsync();
        if (items.Count < size && (items.Count > 0 || page == 1)) return new(items, skip + items.Count);
        return new(items, await query.CountAsync());
    }
}
