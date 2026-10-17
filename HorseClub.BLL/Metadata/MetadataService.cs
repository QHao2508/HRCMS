using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Metadata;

public sealed class MetadataService(CurrentUser current) : IMetadataService
{
    /// <summary>
    /// Trả các tên enum hợp lệ cho client; giá trị API tách khỏi nhãn hiển thị tiếng Việt.
    /// </summary>
    public async Task<Dictionary<string, string[]>> GetEnums()
    {
        await current.Get();
        return typeof(Role).Assembly.GetTypes()
            .Where(t => t.IsEnum && t.Namespace == typeof(Role).Namespace && t != typeof(DatabaseProvider) && t != typeof(EmailDeliveryMode) && t != typeof(ChallengePurpose))
            .OrderBy(t => t.Name).ToDictionary(t => t.Name, t => Enum.GetNames(t));
    }

}
