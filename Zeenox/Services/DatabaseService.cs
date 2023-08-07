using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using Zeenox.Models;

namespace Zeenox.Services;

public class DatabaseService
{
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<GuildConfig> _configs;
    private readonly IMongoCollection<User> _users;
    private readonly DiscordSocketClient _client;

    public DatabaseService(
        IMongoClient mongoClient,
        IConfiguration config,
        IMemoryCache cache,
        DiscordSocketClient client
    )
    {
        _cache = cache;
        _client = client;
        var database = mongoClient.GetDatabase(config.GetSection("MongoDB")["Database"]!);
        _configs = database.GetCollection<GuildConfig>("configs");
        _users = database.GetCollection<User>("users");
    }

    private async Task AddGuildConfigAsync(ulong guildId)
    {
        var cursor = await _configs.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        if (await cursor.AnyAsync().ConfigureAwait(false))
            return;
        var config = new GuildConfig(guildId);
        await _configs.InsertOneAsync(config).ConfigureAwait(false);
    }

    public async Task<GuildConfig> GetGuildConfigAsync(ulong guildId)
    {
        if (_cache.TryGetValue(guildId, out GuildConfig? config))
            return config!;

        await AddGuildConfigAsync(guildId).ConfigureAwait(false);
        var cursor = await _configs.FindAsync(x => x.GuildId == guildId).ConfigureAwait(false);
        var result = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        _cache.Set(guildId, result);
        return result;
    }

    public async Task UpdateGuildConfigAsync(ulong guildId, Action<GuildConfig> action)
    {
        var previous = await GetGuildConfigAsync(guildId).ConfigureAwait(false);
        action(previous);
        await _configs.ReplaceOneAsync(x => x.GuildId == guildId, previous).ConfigureAwait(false);
    }

    private async Task AddUserAsync(ulong userId)
    {
        var cursor = await _users.FindAsync(x => x.UserId == userId).ConfigureAwait(false);
        if (await cursor.AnyAsync().ConfigureAwait(false))
            return;
        var user = new User(userId);
        await _users.InsertOneAsync(user).ConfigureAwait(false);
    }

    public async Task<User> GetUserAsync(ulong userId)
    {
        if (_cache.TryGetValue(userId, out User? user))
            return user!;

        await AddUserAsync(userId).ConfigureAwait(false);
        var cursor = await _users.FindAsync(x => x.UserId == userId).ConfigureAwait(false);
        var result = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        _cache.Set(userId, result);
        return result;
    }

    public async Task UpdateUserAsync(ulong userId, Action<User> action)
    {
        var previous = await GetUserAsync(userId).ConfigureAwait(false);
        action(previous);
        await _users.ReplaceOneAsync(x => x.UserId == userId, previous).ConfigureAwait(false);
        _cache.Set(userId, previous);
    }
}
