namespace Wiley.Apartments.Domain;

/// <summary>Runtime key/value settings editable by clerks (e.g. document vault path on mr-storage).</summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; }
    public string? UpdatedBy { get; set; }
}
