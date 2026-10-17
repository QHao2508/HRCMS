using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Common;

public sealed class PageReader(IOptions<BusinessOptions> options)
{

    public (int Page, int Size) Read(int? page, int? pageSize)
    {
        var p = page ?? 1; var size = pageSize ?? options.Value.DefaultPageSize;
        Ensure.That(size >= 1 && size <= options.Value.MaxPageSize && p >= 1 && p <= int.MaxValue / size, Messages.Get(MessageKey.PageMustBePositiveAndPageSizeBetween1And, options.Value.MaxPageSize));
        return (p, size);
    }

    public async Task<PageResponse<T>> Page<T>(int? page, int? pageSize, Func<int, int, Task<HorseClub.DAL.Abstractions.DataPage<T>>> load)
    {
        var (p, size) = Read(page, pageSize);
        var data = await load(p, size);
        return new(data.Items, p, size, data.Total);
    }

    public static PageResponse<TOut> Map<T, TOut>(PageResponse<T> source, Func<T, TOut> map)
        => new(source.Items.Select(map).ToList(), source.Page, source.PageSize, source.Total);
}
