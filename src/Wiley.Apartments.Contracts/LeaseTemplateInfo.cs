namespace Wiley.Apartments.Contracts;

public sealed record LeaseTemplateInfo(
    string FileName,
    string DisplayName,
    string RelativePath,
    string TermDescription);
