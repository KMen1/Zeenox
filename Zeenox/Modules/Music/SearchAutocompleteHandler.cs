using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Rest.Entities.Tracks;

namespace Zeenox.Modules.Music;

public class SearchAutocompleteHandler(IAudioService audioService) : AutocompleteHandler
{
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

        var results = await audioService.Tracks
            .LoadTracksAsync(
                query,
                new TrackLoadOptions(TrackSearchMode.Spotify, StrictSearchBehavior.Throw)
            )
            .ConfigureAwait(false);

        if (!results.HasMatches)
            return AutocompletionResult.FromSuccess();

        var tracks = results.Tracks.Take(10).Where(x => x.Uri is not null).ToArray();

        var options = tracks
            .Select(x =>
            {
                var title = $"{x.Title} by {x.Author}";
                return title.Length > 100
                    ? new AutocompleteResult(title[..99], x.Uri!.ToString())
                    : new AutocompleteResult(title, x.Uri!.ToString());
            })
            .ToArray();

        return AutocompletionResult.FromSuccess(options);
    }
}
