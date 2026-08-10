using Wiley.Apartments.Contracts;

namespace Wiley.Apartments.Tests.Support;

internal sealed class FixedDocumentPathResolver : IDocumentPathResolver
{
    public FixedDocumentPathResolver(string root)
    {
        ConfiguredDefaultRoot = root;
        _root = root;
    }

    private string _root;

    public string ConfiguredDefaultRoot { get; }

    public string GetDocumentRoot() => _root;

    public Task<string> GetDocumentRootAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_root);

    public Task SetDocumentRootAsync(string absoluteOrRelativePath, string? updatedBy, CancellationToken cancellationToken = default)
    {
        _root = absoluteOrRelativePath;
        Directory.CreateDirectory(_root);
        return Task.CompletedTask;
    }
}
