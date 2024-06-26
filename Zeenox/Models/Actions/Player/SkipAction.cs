﻿using Discord;
using Zeenox.Dtos;
using Zeenox.Models.Player;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class SkipAction(IUser user, ExtendedTrackItem previous, ExtendedTrackItem trackItem)
    : Action(user, ActionType.Skip)
{
    public TrackDTO PreviousTrack { get; } = new(previous);
    public TrackDTO Track { get; } = new(trackItem);
}