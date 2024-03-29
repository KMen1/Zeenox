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
using SpotifyAPI.Web;
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
               config["MongoDB:ConnectionString"]
               ?? throw new Exception("MongoDB connection string is not set!")
           )
       )
       .AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) })
       .AddSingleton<InteractiveService>()
       .AddSingleton<DatabaseService>()
       .AddSingleton<MusicService>()
       .AddMemoryCache()
       .AddHttpClient()
       .AddSingleton(
           new SpotifyClient(
               SpotifyClientConfig
                   .CreateDefault()
                   .WithAuthenticator(
                       new ClientCredentialsAuthenticator(
                           config["Spotify:ClientId"]
                           ?? throw new Exception("Spotify client ID is not set!"),
                           config["Spotify:ClientSecret"]
                           ?? throw new Exception("Spotify client secret is not set!")
                       )
                   )
           )
       )
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
               ValidIssuer =
                   config["JwtSettings:Issuer"] ?? throw new Exception("JWT issuer is not set!"),
               ValidAudience =
                   config["JwtSettings:Audience"] ?? throw new Exception("JWT audience is not set!"),
               IssuerSigningKey = new SymmetricSecurityKey(
                   Encoding.UTF8.GetBytes(
                       config["JwtSettings:Key"] ?? throw new Exception("JWT key is not set!")
                   )
               ),
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = false,
               ValidateIssuerSigningKey = true
           };
       });

builder.Services.AddCors();
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
    app.UseCors(x =>
    {
        x.WithOrigins(config["FrontendUrl"] ?? throw new Exception("Frontend URL is not set!"))
         .AllowAnyMethod();
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRateLimiter();

app.MapControllers();

app.Run();