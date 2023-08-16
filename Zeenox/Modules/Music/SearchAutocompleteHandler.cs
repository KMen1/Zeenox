using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Rest.Entities.Tracks;

namespace Zeenox.Modules.Music;

public class SearchAutocompleteHandler : AutocompleteHandler
{
    private readonly IAudioService _audioService;
    
    public SearchAutocompleteHandler(IAudioService audioService)
    {
        _audioService = audioService;
    }
    
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
        IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter,
        IServiceProvider services
    )
    {
        if (autocompleteInteraction.Data.Current.Value is not string query || query.Length < 3)
            return AutocompletionResult.FromSuccess();

        if (Uri.IsWellFormedUriString(query, UriKind.Absolute))
            return AutocompletionResult.FromSuccess();

        var results = await _audioService.Tracks
            .LoadTracksAsync(
                query,
                new TrackLoadOptions { SearchMode = TrackSearchMode.Spotify, StrictSearch = true }
            )
            .ConfigureAwait(false);

        if (!results.HasMatches)
            return AutocompletionResult.FromSuccess();

        var tracks = results.Tracks.Take(10).Where(x => x.Uri is not null).ToArray();

        var options = tracks
            .Select(x => new AutocompleteResult($"{x.Author} - {x.Title}", x.Uri!.ToString()))
            .ToArray();

        return AutocompletionResult.FromSuccess(options);
    }
}
