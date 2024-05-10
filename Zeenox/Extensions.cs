using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Discord;
using HtmlAgilityPack;
using Serilog;
using Zeenox.Enums;
using Zeenox.Models.Actions;
using Zeenox.Models.Actions.Player;
using Zeenox.Models.Actions.Queue;
using Zeenox.Models.Socket;
using Zeenox.Players;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox;

public static class Extensions
{
    public static string GetMessage(this IAction action, bool includeInfo = false)
    {
        switch (action.Type)
        {
            case ActionType.Play:
                var playAction = (PlayAction)action;
                return "Started playing"
                    + (includeInfo ? $": [{playAction.Track.Title}]({playAction.Track.Url})" : "");
            case ActionType.Rewind:
                var rewindAction = (RewindAction)action;
                return "Started playing"
                    + (
                        includeInfo
                            ? $": [{rewindAction.Track.Title}]({rewindAction.Track.Url})"
                            : ""
                    );
            case ActionType.Skip:
                var skipAction = (SkipAction)action;
                return "Started playing"
                    + (includeInfo ? $": [{skipAction.Track.Title}]({skipAction.Track.Url})" : "");
            case ActionType.SkipTo:
                var skipToAction = (SkipToAction)action;
                return "Started playing"
                    + (
                        includeInfo
                            ? $": [{skipToAction.Track.Title}]({skipToAction.Track.Url})"
                            : ""
                    );
            case ActionType.AddTrack:
                var addTrackAction = (AddTrackAction)action;
                return "Added to queue"
                    + (
                        includeInfo
                            ? $": [{addTrackAction.Track.Title}]({addTrackAction.Track.Url})"
                            : ""
                    );
            case ActionType.AddPlaylist:
                var addPlaylistAction = (AddPlaylistAction)action;
                return "Added to queue"
                    + (
                        includeInfo
                            ? $": [{addPlaylistAction.Playlist?.Name}]({addPlaylistAction.Playlist?.Url})"
                            : ""
                    );
            case ActionType.ClearQueue:
                return "Queue cleared";
            case ActionType.DistinctQueue:
                return "Removed duplicates";
            case ActionType.ReverseQueue:
                return "Reversed queue";
            case ActionType.ShuffleQueue:
                return "Shuffled queue";
            case ActionType.MoveTrack:
                var moveTrackAction = (MoveTrackAction)action;
                return $"Moved to {moveTrackAction.To} from {moveTrackAction.From}";
            case ActionType.RemoveTrack:
                return "Song removed";
            case ActionType.Pause:
                return "Paused playback";
            case ActionType.Resume:
                return "Resumed playback";
            case ActionType.Stop:
                return "Stopped playback";
            case ActionType.Seek:
                var seekAction = (SeekAction)action;
                return $"Seeked to {TimeSpan.FromSeconds(seekAction.Position).ToTimeString()}";
            case ActionType.VolumeUp
            or ActionType.VolumeDown:
                var volumeAction = (VolumeAction)action;
                return $"Volume set to {volumeAction.Volume}%";
            case ActionType.ChangeLoopMode:
                var repeatAction = (RepeatAction)action;
                return repeatAction.TrackRepeatMode switch
                {
                    TrackRepeatMode.None => "Looping disabled",
                    TrackRepeatMode.Track => "Looping current track",
                    TrackRepeatMode.Queue => "Looping queue",
                    _ => "Looping disabled"
                };
            case ActionType.ToggleAutoPlay:
                var toggleAutoPlayAction = (ToggleAutoPlayAction)action;
                return $"Autoplay {(toggleAutoPlayAction.IsAutoPlayEnabled ? "enabled" : "disabled")}";
            default:
                return action.Type.ToString();
        }
    }

    public static string ToTimeString(this TimeSpan timeSpan) =>
        timeSpan.TotalHours < 1
            ? timeSpan.ToString(@"mm\:ss")
            : timeSpan.ToString(timeSpan.TotalDays < 1 ? @"hh\:mm\:ss" : @"dd\:hh\:mm\:ss");

    public static bool TryGetUserId(
        this ClaimsIdentity? claimsPrincipal,
        [NotNullWhen(true)] out ulong? userId
    )
    {
        var result = ulong.TryParse(claimsPrincipal?.FindFirst("USER_ID")?.Value, out var id);
        userId = id;
        return result;
    }

    public static bool TryGetUserId(
        this ClaimsPrincipal? claimsPrincipal,
        [NotNullWhen(true)] out ulong? userId
    )
    {
        var result = ulong.TryParse(claimsPrincipal?.FindFirst("USER_ID")?.Value, out var id);
        userId = id;
        return result;
    }

    public static bool TryGetGuildId(
        this ClaimsIdentity? claimsPrincipal,
        [NotNullWhen(true)] out ulong? guildId
    )
    {
        var result = ulong.TryParse(claimsPrincipal?.FindFirst("GUILD_ID")?.Value, out var id);
        guildId = id;
        return result;
    }

    public static bool TryGetGuildId(
        this ClaimsPrincipal? claimsPrincipal,
        [NotNullWhen(true)] out ulong? guildId
    )
    {
        var result = ulong.TryParse(claimsPrincipal?.FindFirst("GUILD_ID")?.Value, out var id);
        guildId = id;
        return result;
    }

    public static Task SendTextAsync(this WebSocket socket, string text) =>
        socket.SendAsync(
            Encoding.UTF8.GetBytes(text).ToArray(),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );

    public static async Task SendSocketMessagesAsync(
        this WebSocket socket,
        bool updatePlayer,
        bool updateTrack,
        bool updateQueue,
        bool updateActions
    )
    {
        var type = PayloadType.None;
        if (updatePlayer)
        {
            type |= PayloadType.UpdatePlayer;
        }

        if (updateTrack)
        {
            type |= PayloadType.UpdateTrack;
        }

        if (updateQueue)
        {
            type |= PayloadType.UpdateQueue;
        }

        if (updateActions)
        {
            type |= PayloadType.UpdateActions;
        }

        Log.Logger.Debug("Sending socket message with type {Type}", type.ToString());

        await socket
              .SendTextAsync(JsonSerializer.Serialize(new Payload(type)))
              .ConfigureAwait(false);
    }

    public static bool IsUserListening(this SocketPlayer player, IUser user)
    {
        return player.VoiceChannel.ConnectedUsers.Any(x => x.Id == user.Id);
    }

    public static void AddRange<T>(this IList<T>? list, IEnumerable<T>? items)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(items);

        if (list is List<T> asList)
        {
            asList.AddRange(items);
        }
        else
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }
    }

    public static List<string> GetAllText(this HtmlNode node)
    {
        List<string> texts = [];
        var child = node.FirstChild;
        while (child is not null)
        {
            if (child.Name is "br")
            {
                child = child.NextSibling;
                continue;
            }

            if (child.NodeType is HtmlNodeType.Text)
            {
                texts.Add(WebUtility.HtmlDecode(child.InnerText));
            }
            else
            {
                texts.AddRange(GetAllText(child));
            }

            child = child.NextSibling;
        }

        return CombineStringsInParentheses(CombineStringsInParentheses(texts), '[', ']');
    }

    private static List<string> CombineStringsInParentheses(List<string> input, char open = '(', char close = ')')
    {
        var combinedStrings = new List<string>();
        var currentCombinedString = "";

        foreach (var str in input)
        {
            if (str.Contains(open) && str.Contains(close))
            {
                combinedStrings.Add(str);
                continue;
            }

            if (currentCombinedString is not "")
            {
                if (str.Contains(close))
                {
                    currentCombinedString += str;
                    combinedStrings.Add(currentCombinedString);
                    currentCombinedString = "";
                }
                else
                {
                    currentCombinedString += str;
                }
            }
            else
            {
                if (str.Contains(open))
                {
                    currentCombinedString = str;
                }
                else
                {
                    combinedStrings.Add(str);
                }
            }
        }

        return combinedStrings;
    }
}