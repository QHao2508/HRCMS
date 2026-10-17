using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;
using HorseClub.DAL.Abstractions;
namespace HorseClub.BLL.Abstractions.Services;
public interface IManagementService {
 Task<WebsiteResponse> GetWebsite(); Task<WebsiteResponse> UpdateWebsite(WebsiteRequest request);
 Task<WebsiteResponse> UploadAsset(WebsiteAssetKind kind,UploadRequest request);
 Task<OperationResult> OpenAsset(WebsiteAssetKind kind);
 Task<PageResponse<AuditReadModel>> ListAudit(Guid? referenceId,string? search,DateOnly? from,DateOnly? to,int? page,int? pageSize);
}
