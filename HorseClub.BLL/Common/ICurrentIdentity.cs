using System.Security.Claims;

namespace HorseClub.BLL.Common;

public interface ICurrentIdentity
{
    ClaimsPrincipal? Principal { get; }
}
