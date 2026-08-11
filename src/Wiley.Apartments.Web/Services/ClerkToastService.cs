using Wiley.Apartments.Contracts;

namespace Wiley.Apartments.Web.Services;

/// <summary>
/// Bridge so pages can raise toasts; AppToastHost registers the live SfToast show handlers.
/// </summary>
public sealed class ClerkToastService : IClerkToast
{
    private Func<string, string, Task>? _showSuccess;
    private Func<string, string, Task>? _showError;
    private Func<string, string, Task>? _showInfo;

    public void Register(
        Func<string, string, Task> showSuccess,
        Func<string, string, Task> showError,
        Func<string, string, Task> showInfo)
    {
        _showSuccess = showSuccess;
        _showError = showError;
        _showInfo = showInfo;
    }

    public Task ShowSuccessAsync(string message, string title = "Saved") =>
        _showSuccess?.Invoke(title, message) ?? Task.CompletedTask;

    public Task ShowErrorAsync(string message, string title = "Error") =>
        _showError?.Invoke(title, message) ?? Task.CompletedTask;

    public Task ShowInfoAsync(string message, string title = "Notice") =>
        _showInfo?.Invoke(title, message) ?? Task.CompletedTask;
}
