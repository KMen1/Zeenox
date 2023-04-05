using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Logging;
using Lavalink4NET.Tracking;
using Microsoft.AspNetCore.Mvc.Versioning;
using MongoDB.Driver;
using Serilog;
using Zeenox.Services;
using ILogger = Lavalink4NET.Logging.ILogger;

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
                .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
                .AddSingleton<IAudioService, LavalinkNode>()
                .AddSingleton(
                    new LavalinkNodeOptions
                    {
                        AllowResuming = true,
                        DisconnectOnStop = false,
                        WebSocketUri = $"ws://{lavaHost}:{lavaPort}",
                        RestUri = $"http://{lavaHost}:{lavaPort}",
                        Password = lavaPassword
                    }
                )
                .AddSingleton(
                    new InactivityTrackingOptions
                    {
                        DisconnectDelay = TimeSpan.FromMinutes(3),
                        PollInterval = TimeSpan.FromSeconds(5)
                    }
                )
                .AddSingleton<ILogger, EventLogger>()
                .AddSingleton<InactivityTrackingService>()
                .AddSingleton<IMongoClient>(
                    new MongoClient(
                        context.Configuration.GetSection("MongoDB")["ConnectionString"]!
                    )
                )
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

var app = builder.Build();

var client = app.Services.GetRequiredService<DiscordSocketClient>();
var lavalink = app.Services.GetRequiredService<IAudioService>();
client.Ready += async () =>
{
    await lavalink.InitializeAsync();
};

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
