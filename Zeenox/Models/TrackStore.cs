﻿using Zeenox.Models.Player;

namespace Zeenox.Models;

public class TrackStore(string id, ulong? requesterId)
{
    public string Id { get; set; } = id;
    public ulong? RequesterId { get; set; } = requesterId;
    
    public TrackStore(ExtendedTrackItem track) : this(track.Track.Track.ToString(), track.RequestedBy?.Id) { }
}