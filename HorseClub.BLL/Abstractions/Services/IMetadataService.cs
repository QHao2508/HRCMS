using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by MetadataService.</summary>
public interface IMetadataService
{
    Task<Dictionary<string, string[]>> GetEnums();
}
