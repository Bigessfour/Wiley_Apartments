using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Wiley.Apartments.Web.Infrastructure;

/// <summary>
/// Captures the browser Cookie header when a Blazor circuit opens so server-side
/// HttpClient calls (e.g. SfFileManager) can authenticate as the signed-in clerk.
/// HttpContext is unavailable during interactive renders with prerender:false.
/// </summary>
public sealed class CircuitAuthCookieStore
{
    private readonly ConcurrentDictionary<string, string> _cookies = new(StringComparer.Ordinal);

    public void Set(string circuitId, string cookieHeader)
    {
        if (string.IsNullOrWhiteSpace(cookieHeader))
        {
            return;
        }

        _cookies[circuitId] = cookieHeader;
    }

    public bool TryGet(string circuitId, out string? cookieHeader) =>
        _cookies.TryGetValue(circuitId, out cookieHeader);

    public void Remove(string circuitId) => _cookies.TryRemove(circuitId, out _);
}

/// <summary>Scoped lookup of the current circuit's captured auth cookies.</summary>
public sealed class CircuitAuthCookieAccessor(CircuitAuthCookieStore store)
{
    private readonly CircuitAuthCookieStore _store = store;
    private string? _circuitId;

    public void Bind(string circuitId) => _circuitId = circuitId;

    public string? CookieHeader =>
        _circuitId is not null && _store.TryGet(_circuitId, out var cookie)
            ? cookie
            : null;
}

public sealed class CircuitAuthCookieHandler(
    IHttpContextAccessor httpContextAccessor,
    CircuitAuthCookieStore store,
    CircuitAuthCookieAccessor accessor,
    ILogger<CircuitAuthCookieHandler> logger) : CircuitHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly CircuitAuthCookieStore _store = store;
    private readonly CircuitAuthCookieAccessor _accessor = accessor;
    private readonly ILogger<CircuitAuthCookieHandler> _logger = logger;
    private string? _circuitId;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _circuitId = circuit.Id;
        _accessor.Bind(circuit.Id);
        TryCaptureCookies(circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Negotiate/reconnect may have HttpContext when open did not.
        if (_circuitId is not null && !_store.TryGet(_circuitId, out _))
        {
            TryCaptureCookies(_circuitId);
        }

        return Task.CompletedTask;
    }

    private void TryCaptureCookies(string circuitId)
    {
        var cookie = _httpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            _store.Set(circuitId, cookie);
            _logger.LogDebug("Captured auth cookies for Blazor circuit {CircuitId}", circuitId);
        }
        else
        {
            _logger.LogDebug("No cookies available when capturing for circuit {CircuitId}", circuitId);
        }
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _store.Remove(circuit.Id);
        return Task.CompletedTask;
    }
}
