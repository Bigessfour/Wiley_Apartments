using Wiley.Apartments.Contracts;

namespace Wiley.Apartments.Tests.Support;

internal sealed class FixedDocumentPathResolver(string root) : IDocumentPathResolver
{
    private string _root = root;

    public string ConfiguredDefaultRoot { get; } = root;

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
