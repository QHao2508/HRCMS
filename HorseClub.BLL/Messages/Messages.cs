using System.Globalization;
using System.Text.Json;

namespace HorseClub.BLL.Messaging;

/// <summary>Single catalog for validation, notifications and email templates.</summary>
public static class Messages
{
    private static readonly IReadOnlyDictionary<string, string> Catalog = Load();
    public static string Get(MessageKey key, params object?[] arguments)
    {
        var template = Catalog[key.ToString()];
        return arguments.Length == 0 ? template : string.Format(CultureInfo.InvariantCulture, template, arguments);
    }
    private static IReadOnlyDictionary<string, string> Load()
    {
        using var stream = typeof(Messages).Assembly.GetManifestResourceStream("HorseClub.BLL.Messages.messages.en.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }
}
