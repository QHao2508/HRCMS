using System.Text.Json;

namespace HorseClub.BLL.Training;

public static class PublicRacingReference
{
    private static readonly Lazy<JsonElement> Snapshot = new(() =>
    {
        using var stream = typeof(PublicRacingReference).Assembly.GetManifestResourceStream("HorseClub.PublicRacingReference.json")
            ?? throw new InvalidOperationException("Public racing reference data is missing.");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    });
    public static JsonElement Read() => Snapshot.Value;
}
