using System.Text;
using Asp.Versioning;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.Lyrics.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;
using Zeenox;
using Zeenox.Services;

Log.Logger = new LoggerConfiguration().Enrich
    .FromLogContext()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Host.UseSerilog();

builder.Services.AddDiscordHost(
    (socketConfig, _) =>
    {
        socketConfig.SocketConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Verbose,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 200,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
            LogGatewayIntentWarnings = false,
            DefaultRetryMode = RetryMode.AlwaysFail
        };

        socketConfig.Token = config["Discord:Token"]!;
    }
);

builder.Services.AddInteractionService(
    (interactionConfig, _) =>
    {
        interactionConfig.DefaultRunMode = RunMode.Async;
        interactionConfig.LogLevel = LogSeverity.Verbose;
        interactionConfig.UseCompiledLambda = true;
        interactionConfig.LocalizationManager = new JsonLocalizationManager("/", "CommandLocale");
    }
);

builder.Services
    .AddHostedService<InteractionHandler>()
    .AddLavalink()
    .AddInactivityTracking()
    .ConfigureInactivityTracking(x =>
    {
        x.DefaultTimeout = TimeSpan.FromMinutes(3);
        x.TrackingMode = InactivityTrackingMode.Any;
    })
    .AddLyrics()
    .AddSingleton<IMongoClient>(new MongoClient(config["MongoDB:ConnectionString"]))
    .AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) })
    .AddSingleton<InteractiveService>()
    .AddSingleton<DatabaseService>()
    .AddSingleton<MusicService>()
    .AddMemoryCache();

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
            ValidIssuer = config["JwtSettings:Issuer"],
            ValidAudience = config["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)
            ),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddCors(x =>
{
    x.AddPolicy(
        "origin",
        y =>
        {
            y.WithOrigins("zeenox-web.vercel.app");
        }
    );
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
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
    app.UseCors("origin");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

app.Run();
