namespace HorseClub.DAL.Entities;
public sealed class WebsiteSettings : Entity {
 public static readonly Guid PublicId=Guid.Empty;
 public string ContentJson {get;set;}="";
 public string? LogoName {get;set;} public string? LogoType {get;set;}
 public string? HeroName {get;set;} public string? HeroType {get;set;}
 public string? BackgroundName {get;set;} public string? BackgroundType {get;set;}
}
