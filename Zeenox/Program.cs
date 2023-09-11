using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.Lyrics.Extensions;
using Microsoft.AspNetCore.Mvc.Versioning;
using MongoDB.Driver;
using Serilog;
using Zeenox.Services;

Log.Logger = new LoggerConfiguration().Enrich
    .FromLogContext()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseSerilog()
    .ConfigureDiscordHost(
        (context, config) =>
        {
            config.SocketConfig = new DiscordSocketConfig
            {
#if DEBUG
                LogLevel = LogSeverity.Verbose,
#elif RELEASE
                LogLevel = LogSeverity.Info,
#endif
                AlwaysDownloadUsers = true,
                MessageCacheSize = 200,
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
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
#if DEBUG
            config.LogLevel = LogSeverity.Verbose;
#elif RELEASE
            config.LogLevel = LogSeverity.Info;
#endif
            config.UseCompiledLambda = true;
            config.LocalizationManager = new JsonLocalizationManager("/", "CommandLocale");
        }
    )
    .ConfigureServices(
        (context, services) =>
            services
                .AddHostedService<InteractionHandler>()
                .AddLavalink()
                .AddInactivityTracking()
                .ConfigureInactivityTracking(x =>
                {
                    x.DefaultTimeout = TimeSpan.FromMinutes(3);
                    x.TrackingMode = InactivityTrackingMode.Any;
                })
                .AddLyrics()
#if DEBUG
                .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace))
#elif RELEASE
                .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Information))
#endif
                .AddSingleton<IMongoClient>(
                    new MongoClient(
                        context.Configuration.GetSection("MongoDB")["ConnectionString"]!
                    )
                )
                .AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) })
                .AddSingleton<InteractiveService>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<MusicService>()
                .AddMemoryCache()
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

if (app.Environment.IsDevelopment())
{
    app.Run();
}
else
{
    app.Run("http://*:80");
}
