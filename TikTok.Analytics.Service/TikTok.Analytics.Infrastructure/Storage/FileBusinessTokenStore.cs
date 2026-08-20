using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TikTok.Analytics.Application.Configuration;
using TikTok.Analytics.Application.Interfaces;
using TikTok.Analytics.Domain.Entities;

namespace TikTok.Analytics.Infrastructure.Storage;

/// <summary>
/// Business credentials on disk, keyed by BusinessId. Same development-grade caveats as
/// <see cref="FileTikTokTokenStore"/>: plaintext, single-writer, swap for a secret manager
/// before this runs anywhere shared. Only this class changes when that happens.
/// </summary>
public class FileBusinessTokenStore : IBusinessTokenStore
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly ILogger<FileBusinessTokenStore> _logger;

    public FileBusinessTokenStore(
        IOptions<TikTokBusinessOAuthOptions> options,
        IHostEnvironment environment,
        ILogger<FileBusinessTokenStore> logger)
    {
        var configured = options.Value.TokenStorePath;
        _path = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
        _logger = logger;
    }

    public async Task<BusinessToken?> GetByPageAsync(string pageId, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(t => string.Equals(t.PageId, pageId, StringComparison.Ordinal));
    }

    public async Task<IReadOnlyList<BusinessToken>> GetAllAsync(CancellationToken ct = default)
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

    public async Task SaveAsync(BusinessToken token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token.BusinessId))
            throw new ArgumentException("A business token must carry a BusinessId to be stored.", nameof(token));

        await Gate.WaitAsync(ct);
        try
        {
            var tokens = (await ReadUnsafeAsync(ct)).ToList();
            tokens.RemoveAll(t => t.BusinessId == token.BusinessId);
            tokens.Add(token);
            await WriteUnsafeAsync(tokens, ct);

            _logger.LogInformation("Stored business token for business_id {BusinessId}, expires {ExpiresAt:u}",
                token.BusinessId, token.ExpiresAtUtc);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task DeleteAsync(string businessId, CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var tokens = (await ReadUnsafeAsync(ct)).ToList();
            if (tokens.RemoveAll(t => t.BusinessId == businessId) > 0)
            {
                await WriteUnsafeAsync(tokens, ct);
                _logger.LogInformation("Removed business token for business_id {BusinessId}", businessId);
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    // Callers must hold Gate.
    private async Task<List<BusinessToken>> ReadUnsafeAsync(CancellationToken ct)
    {
        if (!File.Exists(_path))
            return [];

        try
        {
            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<List<BusinessToken>>(stream, cancellationToken: ct) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Business token store at {Path} is not valid JSON — treating as empty.", _path);
            return [];
        }
    }

    // Callers must hold Gate. Temp file then move, so a crash mid-write cannot truncate the store.
    private async Task WriteUnsafeAsync(List<BusinessToken> tokens, CancellationToken ct)
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
