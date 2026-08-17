using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;

namespace TikTok.Analytics.Infrastructure.Storage;

/// <summary>
/// Holds pending OAuth state values in process memory.
///
/// Single-instance only: behind a load balancer the callback can land on a different node
/// than the one that issued the state, and the redemption fails. Move to a distributed
/// cache before scaling out.
/// </summary>
public class InMemoryOAuthStateStore : IOAuthStateStore
{
    private sealed record PendingState(DateTime ExpiresAtUtc, string? PageId);

    private readonly ConcurrentDictionary<string, PendingState> _pending = new();
    private readonly TikTokOAuthOptions _options;

    public InMemoryOAuthStateStore(IOptions<TikTokOAuthOptions> options) => _options = options.Value;

    public string Issue(string? pageId = null)
    {
        Prune();
        var state = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        _pending[state] = new PendingState(DateTime.UtcNow.AddMinutes(_options.StateLifetimeMinutes), pageId);
        return state;
    }

    public bool TryRedeem(string state, out string? pageId)
    {
        pageId = null;
        Prune();

        if (string.IsNullOrWhiteSpace(state))
            return false;

        // Single use: removing it is the redemption, so a replayed callback fails.
        if (!_pending.TryRemove(state, out var pending))
            return false;

        if (pending.ExpiresAtUtc <= DateTime.UtcNow)
            return false;

        pageId = pending.PageId;
        return true;
    }

    private void Prune()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in _pending)
        {
            if (entry.Value.ExpiresAtUtc <= now)
                _pending.TryRemove(entry.Key, out _);
        }
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
