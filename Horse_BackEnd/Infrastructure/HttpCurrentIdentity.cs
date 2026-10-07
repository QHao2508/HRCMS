using System.Security.Claims;

namespace Horse_BackEnd.Infrastructure;

public sealed class HttpCurrentIdentity(IHttpContextAccessor accessor) : ICurrentIdentity
{
    public ClaimsPrincipal? Principal => accessor.HttpContext?.User;
}
