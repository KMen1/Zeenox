using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Zeenox.Models;

namespace Zeenox.Services;

public sealed class DatabaseService
{
    private IMongoCollection<GuildConfig> _configs = null!;
    private IMongoCollection<ResumeSession> _resumeSessions = null!;
    private readonly IMongoClient _mongoClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(
        IMongoClient mongoClient,
        IMemoryCache cache,
        ILogger<DatabaseService> logger
    )
    {
        _mongoClient = mongoClient;
        _cache = cache;
        _logger = logger;
        Setup();
    }

    private void Setup()
    {
        _logger.LogInformation("Database service starting...");
        var dbs = _mongoClient.ListDatabaseNames().ToList();
        var db = _mongoClient.GetDatabase("zeenox");

        if (!dbs.Contains("zeenox"))
        {
            db.CreateCollection("guild_configs");
            db.CreateCollection("resume_sessions");
        }

        _configs = db.GetCollection<GuildConfig>("guild_configs");
        _resumeSessions = db.GetCollection<ResumeSession>("resume_sessions");
        _logger.LogInformation("Database service started");
    }

    private async Task AddGuildConfigAsync(ulong guildId)
    {
        var cursor = await _configs.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        if (await cursor.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        var config = new GuildConfig(guildId);
        await _configs.InsertOneAsync(config).ConfigureAwait(false);
        _logger.LogInformation("Guild config added for {GuildId}", guildId);
        _logger.LogDebug("Guild config: {@GuildConfig}", config);
    }

    public async Task<GuildConfig> GetGuildConfigAsync(ulong guildId)
    {
        if (_cache.TryGetValue(guildId, out GuildConfig? config))
        {
            return config!;
        }

        await AddGuildConfigAsync(guildId).ConfigureAwait(false);
        var cursor = await _configs.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        var result = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        _cache.Set(guildId, result, TimeSpan.FromMinutes(5));
        _logger.LogInformation("Got guild config for {GuildId}", guildId);
        _logger.LogDebug("Guild config: {@GuildConfig}", result);
        return result;
    }

    public async Task UpdateGuildConfigAsync(ulong guildId, Action<GuildConfig> action)
    {
        var previous = await GetGuildConfigAsync(guildId).ConfigureAwait(false);
        action(previous);
        await _configs.ReplaceOneAsync(x => x.GuildId == guildId, previous).ConfigureAwait(false);
        _cache.Remove(guildId);
        _cache.Set(guildId, previous, TimeSpan.FromMinutes(5));
        _logger.LogInformation("Guild config updated for {GuildId}", guildId);
        _logger.LogDebug("Guild config: {@GuildConfig}", previous);
    }

    public async Task SaveResumeSessionAsync(ResumeSession session)
    {
        await _resumeSessions
            .ReplaceOneAsync(
                x => x.GuildId == session.GuildId,
                session,
                new ReplaceOptions { IsUpsert = true }
            )
            .ConfigureAwait(false);
        _logger.LogInformation("Resume session saved for {GuildId}", session.GuildId);
        _logger.LogDebug("Resume session: {@ResumeSession}", session);
    }

    public async Task<ResumeSession?> GetResumeSessionAsync(ulong guildId)
    {
        var cursor = await _resumeSessions
            .FindAsync(x => x.GuildId == guildId)
            .ConfigureAwait(false);
        var result = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        _logger.LogInformation("Got resume session for {GuildId}", guildId);
        _logger.LogDebug("Resume session: {@ResumeSession}", result);
        return result;
    }

    public async Task DeleteResumeSessionAsync(ulong guildId)
    {
        await _resumeSessions.DeleteOneAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        _logger.LogInformation("Resume session deleted for {GuildId}", guildId);
    }
}
