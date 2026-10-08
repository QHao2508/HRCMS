using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by BrandingService.</summary>
public interface IBrandingService
{
    Task<Stream> OpenLogo(CancellationToken token);
}
