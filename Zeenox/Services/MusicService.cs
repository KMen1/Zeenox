using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.Players;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using Zeenox.Models;
using Zeenox.Players;

namespace Zeenox.Services;

public partial class MusicService(
    IAudioService audioService,
    DatabaseService databaseService,
    HttpClient httpClient,
    DiscordSocketClient client,
    SpotifyClient spotifyClient,
    IConfiguration config)
{
    private readonly HtmlWeb _htmlWeb = new();

    

    private async Task<T?> TryGetPlayerAsync<T>(ulong guildId)
        where T : class, ILavalinkPlayer
    {
        return await audioService.Players.GetPlayerAsync<T>(guildId).ConfigureAwait(false);
    }

    public Task<SocketPlayer?> TryGetPlayerAsync(ulong guildId)
    {
        return TryGetPlayerAsync<SocketPlayer>(guildId);
    }

    public async ValueTask<SocketPlayer?> TryCreatePlayerAsync(
        ulong guildId,
        SocketVoiceChannel voiceChannel,
        ITextChannel? textChannel = null
    )
    {
        var resumeSession = await databaseService
            .GetResumeSessionAsync(guildId)
            .ConfigureAwait(false);
        var factory = new PlayerFactory<SocketPlayer, SocketPlayerOptions>(
            (properties, _) =>
            {
                properties.Options.Value.TextChannel = textChannel;
                properties.Options.Value.VoiceChannel = voiceChannel;
                properties.Options.Value.DbService = databaseService;
                properties.Options.Value.DiscordClient = client;
                properties.Options.Value.ResumeSession = resumeSession;
                properties.Options.Value.SpotifyClient = spotifyClient;
                properties.Options.Value.AudioService = audioService;
                properties.Options.Value.SpotifyMarket = config["Spotify:Market"] ?? "US";
                return ValueTask.FromResult(new SocketPlayer(properties));
            }
        );

        var retrieveOptions = new PlayerRetrieveOptions(
            ChannelBehavior: PlayerChannelBehavior.Join,
            VoiceStateBehavior: MemberVoiceStateBehavior.RequireSame
        );

        var guildConfig = await databaseService.GetGuildConfigAsync(guildId).ConfigureAwait(false);

        var result = await audioService.Players
            .RetrieveAsync(
                guildId,
                voiceChannel.Id,
                playerFactory: factory,
                options: new OptionsWrapper<SocketPlayerOptions>(
                    new SocketPlayerOptions
                    {
                        SelfDeaf = true,
                        InitialVolume =
                            (float)Math.Floor(guildConfig.MusicSettings.DefaultVolume / (double)2)
                            / 100f,
                        ClearQueueOnStop = false,
                        ClearHistoryOnStop = false,
                    }
                ),
                retrieveOptions
            )
            .ConfigureAwait(false);

        return result.IsSuccess ? result.Player : null;
    }

    public async Task<string?> GetLyricsAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);

        if (player?.CurrentItem is null)
            return null;

        if (player.CurrentItem.Lyrics is not null)
            return player.CurrentItem.Lyrics;

        var lyrics = await FetchLyrics(player.CurrentItem.Track.Track).ConfigureAwait(false);
        player.SetLyrics(lyrics);
        return lyrics;
    }

    private async Task<string?> FetchLyrics(LavalinkTrack track)
    {
        var title = ParenthesesRegex().Replace(track.Title, "").Replace(" ", "+");
        var author = ParenthesesRegex()
            .Replace(track.Author.Split(",").First(), "")
            .Replace(" ", "+");
        var sq = $"{title}+{author}";
        var responseString = await httpClient
            .GetStringAsync($"https://genius.com/api/search/multi?q={sq}")
            .ConfigureAwait(false);
        var geniusObject = JsonConvert.DeserializeObject<Root>(responseString);
        var path = geniusObject?.response.sections
            .Find(x => x.type == "song")
            ?.hits.FirstOrDefault()
            ?.result.path;

        if (path is null)
            return null;

        var url = $"https://genius.com{path}";
        var document = _htmlWeb.Load(url);
        var element = document.DocumentNode.QuerySelectorAll(".Lyrics__Container-sc-1ynbvzw-1");

        foreach (var div in element)
        {
            var children = div.ChildNodes.ToList();
            for (var i = 0; i < children.Count; i++)
            {
                if (children[i].Name == "span")
                {
                    children.Remove(children[i--]);
                    continue;
                }

                if (children[i].Name != "a")
                    continue;

                var newStr = children[i].ChildNodes.ToList().First(x => x.Name == "span").InnerHtml;
                children.Remove(children[i]);

                var newElement = document.CreateTextNode(newStr);
                children.Insert(i, newElement);
            }

            div.ChildNodes.Clear();
            foreach (var child in children)
            {
                div.AppendChild(child);
            }
        }

        var first = element.FirstOrDefault();
        if (first is null || first.ChildNodes.Count == 0)
            return null;

        while (first.ChildNodes[0].Name == "br" || first.ChildNodes[0].InnerText.Contains('['))
        {
            if (first.ChildNodes[0].InnerText.Contains('['))
            {
                while (first.ChildNodes[0].Name != "br")
                    first.ChildNodes.RemoveAt(0);
            }
            first.ChildNodes.RemoveAt(0);
        }

        var rawLyrics = string.Join(' ', element.Select(x => x.InnerHtml));
        var regexed = SectionRegex().Replace(string.Join(' ', rawLyrics), "");
        return regexed;
    }

    [GeneratedRegex(@"\[(.*?)\]")]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"\(([^)]*)\)")]
    private static partial Regex ParenthesesRegex();
}
