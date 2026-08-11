using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Syncfusion.EJ2.FileManager.Base;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Infrastructure;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Web.Controllers;

/// <summary>NAS document-root browse/upload for SfFileManager (FR-019). Mutating ops are audited + metadata-synced.</summary>
[Authorize]
[Route("api/document-vault")]
public sealed class DocumentVaultFileManagerController : Controller
{
    private static readonly HashSet<string> MutatingActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "delete", "rename", "copy", "move", "create", "upload"
    };

    private static readonly JsonSerializerOptions DownloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _documentRoot;
    private readonly PhysicalFileProvider _provider = new();
    private readonly IDocumentVaultAuditService _audit;
    private readonly IDocumentVaultMetadataSync _metadata;
    private readonly ILogger<DocumentVaultFileManagerController> _logger;

    public DocumentVaultFileManagerController(
        IDocumentPathResolver paths,
        IDocumentVaultAuditService audit,
        IDocumentVaultMetadataSync metadata,
        ILogger<DocumentVaultFileManagerController> logger)
    {
        _documentRoot = paths.GetDocumentRoot();
        Directory.CreateDirectory(_documentRoot);
        _provider.RootFolder(_documentRoot);
        _audit = audit;
        _metadata = metadata;
        _logger = logger;
    }

    [HttpPost("FileOperations")]
    [ServiceFilter(typeof(DocumentVaultAntiforgeryFilter))]
    public async Task<object> FileOperations([FromBody] FileManagerDirectoryContent args, CancellationToken cancellationToken)
    {
        DocumentRootAvailability.EnsureWritable(_documentRoot);
        _provider.RootFolder(_documentRoot);

        if (args.Action is "delete" or "rename" && args.TargetPath is null && args.Path == "")
        {
            var response = new FileManagerResponse
            {
                Error = new ErrorDetails { Code = "401", Message = "Restricted to modify the vault root." }
            };
            return _provider.ToCamelCase(response);
        }

        object result = args.Action switch
        {
            "read" => _provider.ToCamelCase(_provider.GetFiles(args.Path, args.ShowHiddenItems)),
            "delete" => _provider.ToCamelCase(_provider.Delete(args.Path, args.Names)),
            "copy" => _provider.ToCamelCase(_provider.Copy(
                args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData)),
            "move" => _provider.ToCamelCase(_provider.Move(
                args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData)),
            "details" => _provider.ToCamelCase(_provider.Details(args.Path, args.Names, args.Data)),
            "create" => _provider.ToCamelCase(_provider.Create(args.Path, args.Name)),
            "search" => _provider.ToCamelCase(_provider.Search(
                args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive)),
            "rename" => _provider.ToCamelCase(_provider.Rename(
                args.Path, args.Name, args.NewName, false, args.ShowFileExtension, args.Data)),
            _ => new object()
        };

        if (MutatingActions.Contains(args.Action ?? string.Empty))
        {
            try
            {
                await AfterMutationAsync(args, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Post-mutation audit/metadata sync failed for {Action}", args.Action);
            }
        }

        return result;
    }

    [HttpPost("Upload")]
    [DisableRequestSizeLimit]
    [ServiceFilter(typeof(DocumentVaultAntiforgeryFilter))]
    public async Task<IActionResult> Upload(
        string path,
        long size,
        IList<IFormFile> uploadFiles,
        string action,
        CancellationToken cancellationToken)
    {
        DocumentRootAvailability.EnsureWritable(_documentRoot);
        _provider.RootFolder(_documentRoot);
        try
        {
            var uploadResponse = _provider.Upload(path, uploadFiles, action, size, null);
            if (uploadResponse.Error is not null)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = int.Parse(uploadResponse.Error.Code);
                Response.HttpContext.Features.Get<IHttpResponseFeature>()!.ReasonPhrase = uploadResponse.Error.Message;
            }
            else
            {
                var names = uploadFiles.Select(f => f.FileName).ToArray();
                await _audit.LogAsync("upload", path, names, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.StatusCode = 417;
            Response.HttpContext.Features.Get<IHttpResponseFeature>()!.ReasonPhrase = ex.Message;
        }

        return Content("");
    }

    [HttpPost("Download")]
    public IActionResult Download([FromForm] string downloadInput)
    {
        _provider.RootFolder(_documentRoot);
        var args = JsonSerializer.Deserialize<FileManagerDirectoryContent>(downloadInput, DownloadJsonOptions)!;
        return _provider.Download(args.Path, args.Names, args.Data);
    }

    [HttpPost("GetImage")]
    public IActionResult GetImage([FromBody] FileManagerDirectoryContent args)
    {
        _provider.RootFolder(_documentRoot);
        return _provider.GetImage(args.Path, args.Id, false, null, null);
    }

    private async Task AfterMutationAsync(FileManagerDirectoryContent args, CancellationToken cancellationToken)
    {
        var action = args.Action ?? string.Empty;
        await _audit.LogAsync(
            action,
            args.Path,
            args.Names ?? (args.Name is null ? null : [args.Name]),
            args.TargetPath,
            args.NewName,
            cancellationToken);

        switch (action.ToLowerInvariant())
        {
            case "delete":
                if (args.Names is { Length: > 0 })
                {
                    await _metadata.SoftDeleteMatchingAsync(args.Path ?? "/", args.Names, cancellationToken);
                }

                break;
            case "rename":
                if (!string.IsNullOrEmpty(args.Name) && !string.IsNullOrEmpty(args.NewName))
                {
                    await _metadata.RenameAsync(args.Path ?? "/", args.Name, args.NewName, cancellationToken);
                }

                break;
            case "move":
                if (args.Names is { Length: > 0 } && args.TargetPath is not null)
                {
                    await _metadata.MoveAsync(args.Path ?? "/", args.TargetPath, args.Names, cancellationToken);
                }

                break;
        }
    }
}
