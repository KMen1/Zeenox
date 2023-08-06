using Lavalink4NET.Tracks;
using Zeenox.Services;

namespace Zeenox.Models;

public class SocketMessage
{
    public ZeenoxPlayer? Player { get; set; }
    public LavalinkTrack? Track { get; set; }
    public List<LavalinkTrack>? Queue { get; set; } = new();
    public int? Position { get; set; }
}
