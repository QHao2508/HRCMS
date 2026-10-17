using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
namespace HorseClub.BLL.Contracts;
public sealed record ReplenishmentSummaryResponse(Guid Id,Guid ItemId,string? ItemName,Guid RequestedBy,decimal Quantity,ReplenishmentStatus Status,string Notes,string? ReviewNotes,decimal ReceivedQuantity,DateTimeOffset? ReceivedAt,DateTimeOffset CreatedAt,long Version)
{
    public static ReplenishmentSummaryResponse From(ReplenishmentRequest r,string? name)=>new(r.Id,r.ItemId,name,r.RequestedBy,r.Quantity,r.Status,r.Notes,r.ReviewNotes,r.ReceivedQuantity,r.ReceivedAt,r.CreatedAt,r.Version);
}
