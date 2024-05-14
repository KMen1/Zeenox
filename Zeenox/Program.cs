using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.Integrations.LyricsJava.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;
using Zeenox;
using Zeenox.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Host.UseSerilog(
    (_, configuration) =>
    {
        configuration.Enrich
            .FromLogContext()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .WriteTo.Console();
    },
    true
);

builder.Services.AddDiscordHost(
    (socketConfig, _) =>
    {
        socketConfig.SocketConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 200,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
            LogGatewayIntentWarnings = false,
            DefaultRetryMode = RetryMode.AlwaysFail
        };

        socketConfig.Token =
            config["Discord:Token"] ?? throw new Exception("Discord token is not set!");
    }
);

builder.Services.AddInteractionService(
    (interactionConfig, _) =>
    {
        interactionConfig.DefaultRunMode = RunMode.Async;
        interactionConfig.LogLevel = LogSeverity.Info;
        interactionConfig.UseCompiledLambda = true;
        interactionConfig.LocalizationManager = new JsonLocalizationManager("/", "CommandLocale");
    }
);

builder.Services
    .AddHostedService<InteractionHandler>()
    .AddLavalink()
    .ConfigureLavalink(x =>
    {
        x.Passphrase =
            config["Lavalink:Password"] ?? throw new Exception("Lavalink password is not set!");
        x.BaseAddress = new Uri(
            config["Lavalink:Host"] ?? throw new Exception("Lavalink host is not set!")
        );
    })
    .AddInactivityTracking()
    .ConfigureInactivityTracking(x =>
    {
        x.DefaultTimeout = TimeSpan.FromMinutes(3);
        x.TrackingMode = InactivityTrackingMode.Any;
    })
    .AddSingleton<IMongoClient>(
        new MongoClient(
            config["MongoConnectionString"]
                ?? throw new Exception("MongoDB connection string is not set!")
        )
    )
    .AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) })
    .AddSingleton<InteractiveService>()
    .AddSingleton<DatabaseService>()
    .AddSingleton<MusicService>()
    .AddMemoryCache()
    .AddHttpClient()
    .AddLogging(x => x.AddSerilog())
    .AddRateLimiter(x =>
    {
        x.AddTokenBucketLimiter(
            "global",
            y =>
            {
                y.TokenLimit = 6;
                y.TokensPerPeriod = 2;
                y.ReplenishmentPeriod = TimeSpan.FromSeconds(2);
                y.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
                y.QueueLimit = 0;
                y.AutoReplenishment = true;
            }
        );
        x.RejectionStatusCode = 429;
    });

builder.Services
    .AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "Zeenox-API",
            ValidAudience = "Zeenox-Client",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    config["JwtKey"] ?? throw new Exception("JWT key is not set!")
                )
            ),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddApiVersioning(o =>
    {
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.DefaultApiVersion = new ApiVersion(1, 0);
        o.ReportApiVersions = true;
        o.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddRouting(x => x.LowercaseUrls = true);

builder.Services.AddHttpsRedirection(x =>
{
    x.RedirectStatusCode = 308;
    x.HttpsPort = 443;
});

builder.WebHost.ConfigureKestrel(x =>
{
    x.Listen(IPAddress.Any, 80);
    x.Listen(IPAddress.Any, 443, options => options.UseHttps());
});

var app = builder.Build();

app.UseLyricsJava(x => x.AutoResolve = true);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRateLimiter();

app.MapControllers();

app.Run();
