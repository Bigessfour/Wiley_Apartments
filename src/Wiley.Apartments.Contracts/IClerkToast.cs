namespace Wiley.Apartments.Contracts;

public interface IClerkToast
{
    Task ShowSuccessAsync(string message, string title = "Saved");
    Task ShowErrorAsync(string message, string title = "Error");
    Task ShowInfoAsync(string message, string title = "Notice");
}
