using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;
namespace Horse_BackEnd.Endpoints;
public static class WebsiteEndpoints {
 public static void MapWebsite(this RouteGroupBuilder api){
  var group=api.MapGroup("/website").WithTags("Website");
  group.MapGet("",async(IManagementService service)=>await service.GetWebsite()).AllowAnonymous().Produces<WebsiteResponse>();
  group.MapPut("",async(WebsiteRequest request,IManagementService service)=>await service.UpdateWebsite(request)).RequireAuthorization().Produces<WebsiteResponse>();
  group.MapPost("/assets/{kind}",async(WebsiteAssetKind kind,HttpRequest request,IManagementService service)=>await service.UploadAsset(kind,UploadRequestAdapter.Create(request))).RequireAuthorization().RequireRateLimiting("uploads").Produces<WebsiteResponse>();
  group.MapGet("/assets/{kind}",async(WebsiteAssetKind kind,IManagementService service)=>(await service.OpenAsset(kind)).ToHttpResult()).AllowAnonymous().Produces<byte[]>(200,contentType:"application/octet-stream");
 }
}
