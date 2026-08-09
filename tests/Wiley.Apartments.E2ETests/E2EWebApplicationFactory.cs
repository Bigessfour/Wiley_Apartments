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

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{webProject}\" --urls {E2EBaseUrl} --no-launch-profile",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _process.Start();

        var deadline = DateTime.UtcNow.AddSeconds(45);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        while (DateTime.UtcNow < deadline)
        {
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
