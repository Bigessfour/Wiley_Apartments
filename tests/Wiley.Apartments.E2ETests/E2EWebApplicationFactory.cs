using System.Diagnostics;
using System.Net;

namespace Wiley.Apartments.E2ETests;

public sealed class E2EWebApplicationFactory : IAsyncLifetime, IDisposable
{
    private Process? _process;

    public string E2EBaseUrl { get; } = "http://127.0.0.1:5199";

    public async Task InitializeAsync()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var webProject = Path.Combine(repoRoot, "src", "Wiley.Apartments.Web", "Wiley.Apartments.Web.csproj");
        var dotnet = Environment.GetEnvironmentVariable("DOTNET_ROOT") is { Length: > 0 } root
            ? Path.Combine(root, "dotnet")
            : "dotnet";
        if (!File.Exists(dotnet) && File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "dotnet")))
        {
            dotnet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "dotnet");
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = dotnet,
                Arguments = $"run --project \"{webProject}\" --urls {E2EBaseUrl} --no-launch-profile",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        // Match launchSettings "http" profile so license is optional and static assets resolve.
        _process.StartInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        _process.StartInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
        // Empty placeholder is enough for Development (bootstrap warns, does not throw).
        _process.StartInfo.Environment["SYNCFUSION_LICENSE_KEY"] =
            Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") ?? "";
        // Isolated SQLite so E2E never mutates a developer's local clerksuite.db
        var e2eDb = Path.Combine(repoRoot, "src", "Wiley.Apartments.Web", "Data", "clerksuite-e2e.db");
        try
        {
            if (File.Exists(e2eDb))
            {
                File.Delete(e2eDb);
            }
        }
        catch
        {
            // best-effort clean slate
        }

        _process.StartInfo.Environment["ConnectionStrings__DefaultConnection"] = $"Data Source={e2eDb}";

        // Ensure child sees the same SDK as the test host.
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var dotnetDir = Path.GetDirectoryName(dotnet);
        if (!string.IsNullOrEmpty(dotnetDir) && !path.Split(Path.PathSeparator).Contains(dotnetDir))
        {
            _process.StartInfo.Environment["PATH"] = $"{dotnetDir}{Path.PathSeparator}{path}";
            _process.StartInfo.Environment["DOTNET_ROOT"] = dotnetDir;
        }

        _process.Start();

        // Drain logs so the process does not block on full pipes.
        _ = Task.Run(() =>
        {
            try
            {
                while (_process is { HasExited: false } && _process.StandardOutput.ReadLine() is not null)
                {
                }
            }
            catch
            {
                // ignored
            }
        });
        _ = Task.Run(() =>
        {
            try
            {
                while (_process is { HasExited: false } && _process.StandardError.ReadLine() is not null)
                {
                }
            }
            catch
            {
                // ignored
            }
        });

        var deadline = DateTime.UtcNow.AddSeconds(60);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"E2E host exited early (code {_process.ExitCode}) before binding {E2EBaseUrl}.");
            }

            try
            {
                var response = await client.GetAsync($"{E2EBaseUrl}/Account/Login");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch
            {
                // Host still starting
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException("E2E host failed to start on " + E2EBaseUrl);
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }
}
