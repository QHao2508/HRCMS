using HorseClub.BLL.Messaging;
using System.Security.Claims;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Common;

public sealed class CurrentUser(IAccessRepository repository, ICurrentIdentity identity)
{
    private User? cached;
    /// <summary>
    /// Đọc danh tính request, tra user và kiểm trạng thái/stamp; dùng lại kết quả trong scope để không tra user lặp lại.
    /// </summary>
    public async Task<User> Get()
    {
        if (cached is not null) return cached;
        var principal = identity.Principal;
        Ensure.That(Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id), Messages.Get(MessageKey.AuthenticationRequired), 401, "unauthorized");
        var user = await repository.FindUserAsync(id);
        Ensure.That(user is { Active: true, EmailVerified: true } && principal?.FindFirstValue("stamp") == user.SecurityStamp,
            Messages.Get(MessageKey.AccountOrTokenIsNoLongerValid), 401, "unauthorized");
        return cached = user!;
    }
}
