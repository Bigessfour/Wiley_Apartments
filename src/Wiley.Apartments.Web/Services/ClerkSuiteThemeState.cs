using Syncfusion.Blazor;

namespace Wiley.Apartments.Web.Services;

/// <summary>
/// Per-circuit light/dark flag. Charts paint SVG from <see cref="ChartTheme"/> at render time,
/// so pages must remount viz when this changes — swapping the Fluent CSS file is not enough.
/// </summary>
public sealed class ClerkSuiteThemeState
{
    public bool IsDark { get; private set; }

    public Theme ChartTheme => IsDark ? Theme.Fluent2Dark : Theme.Fluent2;

    public string AccentColor => IsDark ? "#62b0f0" : "#0f6cbd";

    public string GaugeTrackColor => IsDark ? "#3d4f63" : "#c5d8f0";

    public string GaugeLabelColor => IsDark ? "#f3f2f1" : "#242424";

    public event Action? Changed;

    public void SetDark(bool isDark)
    {
        if (IsDark == isDark)
        {
            return;
        }

        IsDark = isDark;
        Changed?.Invoke();
    }
}
