namespace HorseClub.BLL.Common;

public sealed record UploadForm(IReadOnlyDictionary<string, string> Fields, UploadFiles Files)
{
    public string this[string name] => Fields.GetValueOrDefault(name, "");
}
