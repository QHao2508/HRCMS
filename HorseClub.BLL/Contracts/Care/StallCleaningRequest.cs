using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

/// <summary>Trạng thái vệ sinh ô chuồng do Manager/Groom chọn (Clean hoặc NeedsCleaning).</summary>
public sealed record StallCleaningRequest(CleaningStatus Status);
