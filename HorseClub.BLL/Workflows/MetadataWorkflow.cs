using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;

namespace HorseClub.BLL.Workflows;

public static class MetadataWorkflow
{
    public static async Task<object> GetMetadataEnums(CurrentUser current)
    {
            await current.Get();
            return typeof(Role).Assembly.GetTypes()
                .Where(t => t.IsEnum && t.Namespace == typeof(Role).Namespace && t != typeof(DatabaseProvider) && t != typeof(EmailDeliveryMode) && t != typeof(ChallengePurpose))
                .OrderBy(t => t.Name).ToDictionary(t => t.Name, t => Enum.GetNames(t));
        }

}
