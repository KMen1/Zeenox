using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Artwork;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.Lyrics.Extensions;
using Lavalink4NET.Tracking;
using Microsoft.AspNetCore.Mvc.Versioning;
using MongoDB.Driver;
using Serilog;
using SpotifyAPI.Web;
using Zeenox.Services;

Log.Logger = new LoggerConfiguration().Enrich
    .FromLogContext()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
var lavaHost = builder.Configuration.GetSection("Lavalink")["Host"]!;
var lavaPort = builder.Configuration.GetSection("Lavalink")["Port"]!;
var lavaPassword = builder.Configuration.GetSection("Lavalink")["Password"]!;

builder.Host
    .UseSerilog()
    .ConfigureDiscordHost(
        (context, config) =>
        {
            config.SocketConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 200,
                GatewayIntents = GatewayIntents.All,
                LogGatewayIntentWarnings = false,
                DefaultRetryMode = RetryMode.AlwaysFail
            };
            config.Token = context.Configuration.GetSection("Discord")["Token"]!;
        }
    )
    .UseInteractionService(
        (_, config) =>
        {
            config.DefaultRunMode = RunMode.Async;
            config.LogLevel = LogSeverity.Verbose;
            config.UseCompiledLambda = true;
            //config.LocalizationManager = new JsonLocalizationManager("Resources", "DCLocalization");
        }
    )
    .ConfigureServices(
        (context, services) =>
        {
            services
                .AddHostedService<InteractionHandler>()
                .AddLavalink()
                .AddInactivityTracking()
                .AddLyrics()
                .AddSingleton<IArtworkService, ArtworkService>()
                .Configure<InactivityTrackingOptions>(x =>
                {
                    x.DisconnectDelay = TimeSpan.FromMinutes(3);
                    x.TrackInactivity = true;
                })
                .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace))
                .AddSingleton<IMongoClient>(
                    new MongoClient(
                        context.Configuration.GetSection("MongoDB")["ConnectionString"]!
                    )
                )
                .AddSingleton(
                    new SpotifyClient(
                        SpotifyClientConfig
                            .CreateDefault()
                            .WithAuthenticator(
                                new ClientCredentialsAuthenticator(
                                    context.Configuration.GetSection("Spotify")["CLIENT_ID"]!,
                                    context.Configuration.GetSection("Spotify")["CLIENT_SECRET"]!
                                )
                            )
                    )
                )
                .AddSingleton<SpotifyService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<MusicService>()
                .AddMemoryCache();
        }
    );

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApiVersioning(o =>
{
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    o.ReportApiVersions = true;
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
});
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddRouting(x => x.LowercaseUrls = true);
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseWebSockets();
app.MapControllers();

app.Run();
