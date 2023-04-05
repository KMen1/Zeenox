using Discord;
using Zeenox.Services;

namespace Zeenox.Modules.Music;

public class MusicBase : ModuleBase
{
    public MusicService MusicService { get; set; } = null!;
    public DatabaseService DatabaseService { get; set; } = null!;
    public IVoiceState? VoiceState => Context.User as IVoiceState;
}
