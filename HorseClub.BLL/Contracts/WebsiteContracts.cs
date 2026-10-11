namespace HorseClub.BLL.Contracts;
public sealed record WebsiteFeature(string Title,string Description);
public sealed record WebsiteContent(string HeroTitle,string HeroDescription,string FeaturesTitle,string FeaturesDescription,List<WebsiteFeature> Features);
public sealed record WebsiteRequest(long Version,string HeroTitle,string HeroDescription,string FeaturesTitle,string FeaturesDescription,List<WebsiteFeature> Features);
public sealed record WebsiteResponse(long Version,string HeroTitle,string HeroDescription,string FeaturesTitle,string FeaturesDescription,List<WebsiteFeature> Features,string? LogoVersion,string? HeroImageVersion,string? BackgroundImageVersion);
