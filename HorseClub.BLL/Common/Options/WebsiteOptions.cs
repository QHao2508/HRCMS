using HorseClub.BLL.Contracts;
namespace HorseClub.BLL.Common;
public sealed class WebsiteOptions {
 public WebsiteContent Defaults {get;set;}=new("","","","",[]);
 public int TitleMaxLength {get;set;}=160; public int DescriptionMaxLength {get;set;}=1000;
 public int FeatureCount {get;set;}=3;
}
