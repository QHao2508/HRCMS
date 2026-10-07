using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Common;

public sealed class BrandingOptions
{
    public const string Section = "Branding";
    [Required, RegularExpression(@"^branding/hrcms-logo-[a-f0-9]{16}\.png$")]
    public string LogoBlobName { get; set; } = "branding/hrcms-logo-153411c4096984a2.png";
}
