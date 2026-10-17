namespace HorseClub.DAL.Entities;

/// <summary>External evidence, separate from club horses, clinical records and stock.</summary>
public sealed class PublicReferenceRecord : Entity
{
    public string ExternalKey { get; set; } = "";
    public string Kind { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public DateOnly CollectedOn { get; set; }
    public DateOnly? SourceAsOf { get; set; }
    public string SourceMetadataJson { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public string ContentSha256 { get; set; } = "";
}
