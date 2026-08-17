using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Infrastructure.Storage;

/// <summary>
/// Persists token sets to a JSON file on disk, guarded by a process-wide lock.
///
/// This is a development-grade store. It holds live credentials in plaintext and assumes
/// a single instance owns the file, so before this runs anywhere shared it should be
/// swapped for a secret manager (Azure Key Vault, AWS Secrets Manager) or an encrypted
/// column in a real database. Only this class changes — everything else depends on
/// ITikTokTokenStore.
/// </summary>
public class FileTikTokTokenStore : ITikTokTokenStore
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly ILogger<FileTikTokTokenStore> _logger;

    public FileTikTokTokenStore(
        IOptions<TikTokOAuthOptions> options,
        IHostEnvironment environment,
        ILogger<FileTikTokTokenStore> logger)
    {
        var configured = options.Value.TokenStorePath;
        _path = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
        _logger = logger;
    }

    public async Task<TikTokToken?> GetAsync(string openId, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(t => t.OpenId == openId);
    }

    public async Task<IReadOnlyList<TikTokToken>> GetAllAsync(CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            return await ReadUnsafeAsync(ct);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task SaveAsync(TikTokToken token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token.OpenId))
            throw new ArgumentException("A token must carry an OpenId to be stored.", nameof(token));

        await Gate.WaitAsync(ct);
        try
        {
            var tokens = (await ReadUnsafeAsync(ct)).ToList();
            tokens.RemoveAll(t => t.OpenId == token.OpenId);
            tokens.Add(token);
            await WriteUnsafeAsync(tokens, ct);

            _logger.LogInformation("Stored TikTok token for open_id {OpenId}, expires {ExpiresAt:u}",
                token.OpenId, token.ExpiresAtUtc);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task DeleteAsync(string openId, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var tokens = (await ReadUnsafeAsync(ct)).ToList();
            if (tokens.RemoveAll(t => t.OpenId == openId) > 0)
            {
                await WriteUnsafeAsync(tokens, ct);
                _logger.LogInformation("Removed TikTok token for open_id {OpenId}", openId);
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    // Callers must hold Gate.
    private async Task<List<TikTokToken>> ReadUnsafeAsync(CancellationToken ct)
    {
        if (!File.Exists(_path))
            return [];

        try
        {
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<List<TikTokToken>>(stream, cancellationToken: ct) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Token store at {Path} is not valid JSON — treating as empty. " +
                                 "Existing tokens will be overwritten on the next save.", _path);
            return [];
        }
    }

    // Callers must hold Gate. Writes via a temp file so a crash mid-write cannot truncate the store.
    private async Task WriteUnsafeAsync(List<TikTokToken> tokens, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, tokens, JsonOptions, ct);
        }

        File.Move(temp, _path, overwrite: true);
    }
}
