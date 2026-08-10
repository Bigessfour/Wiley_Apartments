using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Wiley.Apartments.Web.Infrastructure;

/// <summary>
/// Logs Blazor Interactive Server circuit lifecycle and connection drops so
/// UI "unhandled error" banners can be correlated with server logs.
/// </summary>
public sealed class LoggingCircuitHandler : CircuitHandler
{
    private readonly ILogger<LoggingCircuitHandler> _logger;

    public LoggingCircuitHandler(ILogger<LoggingCircuitHandler> logger) => _logger = logger;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blazor circuit opened {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Blazor circuit connection up {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Blazor circuit connection down {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blazor circuit closed {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
