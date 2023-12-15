﻿using Discord;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions;

public interface IAction
{
    IUser User { get; }
    ActionType Type { get; }
    DateTimeOffset Timestamp { get; }

    string Stringify();
}
