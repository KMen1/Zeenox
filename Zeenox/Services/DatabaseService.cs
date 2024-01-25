using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Serilog;
using Zeenox.Models;

namespace Zeenox.Services;

public sealed class DatabaseService
{
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<GuildConfig> _configs;
    private readonly IMongoCollection<PlayerResumeSession> _resumeSessions;

    public DatabaseService(IMongoClient mongoClient, IConfiguration config, IMemoryCache cache)
    {
        _cache = cache;
        var database = mongoClient.GetDatabase(
            config["MongoDB:Database"] ?? throw new Exception("MongoDB database name is not set!")
        );
        _configs = database.GetCollection<GuildConfig>(
            config["MongoDB:ConfigCollection"]
                ?? throw new Exception("MongoDB config collection name is not set!")
        );
        _resumeSessions = database.GetCollection<PlayerResumeSession>(
            config["MongoDB:ResumeSessionCollection"]
                ?? throw new Exception("MongoDB resume session collection name is not set!")
        );
    }

    private async Task AddGuildConfigAsync(ulong guildId)
    {
        var cursor = await _configs.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        if (await cursor.AnyAsync().ConfigureAwait(false))
            return;
        var config = new GuildConfig(guildId);
        await _configs.InsertOneAsync(config).ConfigureAwait(false);
        Log.Logger.Information("Added config for guild with id: {GuildId}", guildId);
    }

    public async Task<GuildConfig> GetGuildConfigAsync(ulong guildId)
    {
        if (_cache.TryGetValue(guildId, out GuildConfig? config))
            return config!;

        await AddGuildConfigAsync(guildId).ConfigureAwait(false);
        var cursor = await _configs.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        var result = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        _cache.Set(guildId, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task UpdateGuildConfigAsync(ulong guildId, Action<GuildConfig> action)
    {
        var previous = await GetGuildConfigAsync(guildId).ConfigureAwait(false);
        action(previous);
        await _configs.ReplaceOneAsync(x => x.GuildId == guildId, previous).ConfigureAwait(false);
        _cache.Remove(guildId);
        _cache.Set(guildId, previous, TimeSpan.FromMinutes(5));
        Log.Logger.Information("Updated config for guild with id: {GuildId}", guildId);
    }

    public async Task SaveResumeSessionAsync(PlayerResumeSession session)
    {
        await _resumeSessions
            .ReplaceOneAsync(
                x => x.GuildId == session.GuildId,
                session,
                new ReplaceOptions { IsUpsert = true }
            )
            .ConfigureAwait(false);
        Log.Logger.Information(
            "Saved resume session for guild with id: {GuildId}",
            session.GuildId
        );
    }

    public async Task<PlayerResumeSession?> GetResumeSessionAsync(ulong guildId)
    {
        var cursor = await _resumeSessions
            .FindAsync(x => x.GuildId == guildId)
            .ConfigureAwait(false);
        var result = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        Log.Logger.Information("Got resume session for guild with id: {GuildId}", guildId);

        //if (result is not null)
        //    await _resumeSessions.DeleteOneAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        return result;
    }

    public async Task DeleteResumeSessionAsync(ulong guildId)
    {
        await _resumeSessions.DeleteOneAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        Log.Logger.Information("Deleted resume session for guild with id: {GuildId}", guildId);
    }

    public async Task<IEnumerable<PlayerResumeSession>> GetResumeSessionsAsync(
        params ulong[] guildId
    )
    {
        var cursor = await _resumeSessions
            .FindAsync(x => guildId.Contains(x.GuildId))
            .ConfigureAwait(false);
        var result = await cursor.ToListAsync().ConfigureAwait(false);
        Log.Logger.Information(
            "Got resume sessions for guilds with ids: {GuildIds}",
            string.Join(", ", guildId)
        );
        return result;
    }
}
