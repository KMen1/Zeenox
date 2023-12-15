using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Serilog;
using Zeenox.Models;

namespace Zeenox.Services;

public sealed class DatabaseService
{
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<GuildConfig> _configs;

    public DatabaseService(IMongoClient mongoClient, IConfiguration config, IMemoryCache cache)
    {
        _cache = cache;
        var database = mongoClient.GetDatabase(config.GetSection("MongoDB")["Database"]!);
        _configs = database.GetCollection<GuildConfig>("configs");
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
        Log.Logger.Information("Updated config for guild with id:  {GuildId}", guildId);
    }
}
