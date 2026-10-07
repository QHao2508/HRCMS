using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Common;

public sealed class PageReader(IOptions<BusinessOptions> options)
{
    /// <summary>
    /// Kiểm tra page/pageSize, đếm tổng và đọc một trang dữ liệu không tracking; tránh tải cả bảng để phân trang.
    /// </summary>
    /// <param name="q">Giá trị kiểu IQueryable&lt;T&gt; dùng trong Page.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong Page.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong Page.</param>
    public async Task<PageResponse<T>> Page<T>(IQueryable<T> q, int? page, int? pageSize) where T : class
    {
        var p = page ?? 1; var size = pageSize ?? options.Value.DefaultPageSize;
        Ensure.That(size >= 1 && size <= options.Value.MaxPageSize && p >= 1 && p <= int.MaxValue / size, Messages.Get(MessageKey.PageMustBePositiveAndPageSizeBetween1And, options.Value.MaxPageSize));
        return new PageResponse<T>(await q.AsNoTracking().Skip((p - 1) * size).Take(size).ToListAsync(), p, size, await q.CountAsync());
    }
}
