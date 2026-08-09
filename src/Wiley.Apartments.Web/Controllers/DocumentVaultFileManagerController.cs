using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Syncfusion.EJ2.FileManager.Base;
using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using Wiley.Apartments.Web.Configuration;

namespace Wiley.Apartments.Web.Controllers;

/// <summary>NAS document-root browse/upload for SfFileManager (FR-019).</summary>
[Authorize]
[Route("api/document-vault")]
[IgnoreAntiforgeryToken]
public sealed class DocumentVaultFileManagerController : Controller
{
    private readonly string _documentRoot;
    private readonly PhysicalFileProvider _provider = new();

    public DocumentVaultFileManagerController(
        IHostEnvironment environment,
        IOptions<ClerkSuiteOptions> options)
    {
        var root = options.Value.DocumentRoot;
        _documentRoot = Path.IsPathRooted(root)
            ? root
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, root));
        Directory.CreateDirectory(_documentRoot);
        _provider.RootFolder(_documentRoot);
    }

    [HttpPost("FileOperations")]
    public object FileOperations([FromBody] FileManagerDirectoryContent args)
    {
        _provider.RootFolder(_documentRoot);
        if (args.Action is "delete" or "rename" && args.TargetPath is null && args.Path == "")
        {
            var response = new FileManagerResponse
            {
                Error = new ErrorDetails { Code = "401", Message = "Restricted to modify the vault root." }
            };
            return _provider.ToCamelCase(response);
        }

        switch (args.Action)
        {
            case "read":
                return _provider.ToCamelCase(_provider.GetFiles(args.Path, args.ShowHiddenItems));
            case "delete":
                return _provider.ToCamelCase(_provider.Delete(args.Path, args.Names));
            case "copy":
                return _provider.ToCamelCase(_provider.Copy(
                    args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
            case "move":
                return _provider.ToCamelCase(_provider.Move(
                    args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
            case "details":
                return _provider.ToCamelCase(_provider.Details(args.Path, args.Names, args.Data));
            case "create":
                return _provider.ToCamelCase(_provider.Create(args.Path, args.Name));
            case "search":
                return _provider.ToCamelCase(_provider.Search(
                    args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive));
            case "rename":
                return _provider.ToCamelCase(_provider.Rename(
                    args.Path, args.Name, args.NewName, false, args.ShowFileExtension, args.Data));
        }

        return new object();
    }

    [HttpPost("Upload")]
    [DisableRequestSizeLimit]
    public IActionResult Upload(string path, long size, IList<IFormFile> uploadFiles, string action)
    {
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
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var args = JsonSerializer.Deserialize<FileManagerDirectoryContent>(downloadInput, options)!;
        return _provider.Download(args.Path, args.Names, args.Data);
    }

    [HttpPost("GetImage")]
    public IActionResult GetImage([FromBody] FileManagerDirectoryContent args)
    {
        _provider.RootFolder(_documentRoot);
        return _provider.GetImage(args.Path, args.Id, false, null, null);
    }
}
