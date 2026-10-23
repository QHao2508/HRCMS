using System.Text.Json;
using HorseClub.DAL.Entities;

namespace HorseClub.BLL.Messaging;

/// <summary>Readable Vietnamese copy without changing legacy stored messages or notification type values.</summary>
public static class NotificationPresentation
{
    private static readonly IReadOnlyDictionary<string, string> Display = Load();
    private static IReadOnlyDictionary<string, string> Load()
    {
        using var stream = typeof(Messages).Assembly.GetManifestResourceStream("HorseClub.BLL.Messages.notifications.vi.json")!;
        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
        if (result.Any(pair => !Enum.TryParse<MessageKey>(pair.Key, out _) || string.IsNullOrWhiteSpace(pair.Value)))
            throw new InvalidOperationException("Invalid notification display catalogue.");
        return result;
    }
    public static Notification Enrich(Notification notification)
    {
        var match = Display.FirstOrDefault(pair => notification.Message == Messages.Get(Enum.Parse<MessageKey>(pair.Key)) || notification.Message == pair.Value);
        notification.MessageCode = match.Key;
        notification.DisplayMessage = match.Key is null ? notification.Message : match.Value;
        return notification;
    }
}
