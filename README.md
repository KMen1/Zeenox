# Zeenox

[![Discord](https://discordapp.com/api/guilds/863751874922676234/widget.png)](https://discord.gg/hGxaMkfMBR)
[![License](https://img.shields.io/github/license/kmen1/Zeenox)](https://github.com/KMen1/Zeenox/blob/main/LICENSE)
[![CodeFactor](https://www.codefactor.io/repository/github/kmen1/zeenox/badge)](https://www.codefactor.io/repository/github/kmen1/zeenox)
[![Deploy](https://github.com/KMen1/Zeenox/actions/workflows/deploy.yml/badge.svg)](https://github.com/KMen1/Zeenox/actions/workflows/deploy.yml)

A Discord music bot that focuses on speed and ease of usability, with a web dashboard, all for completely free. Allows playback from Spotify, YouTube, SoundCloud, BandCamp, Twitch

# Features

- Comes with a fully functional **web interface** that allows you to control the bot from your browser. For more information [click here](https://github.com/KMen1/Zeenox-Web)
- Can be used from discord, web or both
- **Automatically saves any session that hasn't been fully completed**, so you can resume listening later exactly where you left off
- **Keeps track of every action** that is performed and allows you to view who performed it and when
- Whitelist roles, users and channels
- **Exclusive mode** - Limit usage to current song requester
- **Autoplay** - plays recommended songs after the queue is empty
- **Lyrics** - Shows the lyrics for the currently playing song (_only available on web_)
- **Autocomplete search** - The _play_ command uses autocomplete to let you search for songs
- **Loop modes** - Current song or queue
- Ability to shuffle, clear, reverse, remove songs or duplicates from the queue

# Setup

### Requirements

- .NET 8
- Running instance of [Lavalink](https://github.com/lavalink-devs/Lavalink) with [LavaSrc](https://github.com/topi314/LavaSrc), [LavaSearch](https://github.com/topi314/LavaSearch), [youtube-source](https://github.com/lavalink-devs/youtube-source) [LavaLyrics](https://github.com/topi314/LavaLyrics), [Lyrics.Java](https://github.com/DuncteBot/java-timed-lyrics)
- Discord OAuth application, [click here](https://discord.com/developers/applications) to create one
- MongoDB database

### Installation

1. Clone the repository

```bash
git clone https://github.com/KMen1/Zeenox.git
```

2. Build project

```bash
dotnet restore "Zeenox/Zeenox.csproj"
dotnet build "Zeenox/Zeenox.csproj" -c Release -o /app/build
dotnet publish "Zeenox/Zeenox.csproj" -c Release -o /app/publish /p:UseAppHost=false
```

3. Edit appsettings.json in publish folder

> [!IMPORTANT]
> All values must be provided in order for the bot to start and function correctly!

```json
{
  "JwtKey": "",
  "MongoConnectionString": "",
  "Discord": {
    "Token": "",
    "Activity": "/play"
  },
  "Lavalink": {
    "Host": "http://localhost:2333",
    "Password": "youshallnotpass"
  },
  "AllowedHosts": "*"
}
```

4. Run the bot from the publish folder

```bash
dotnet Zeenox.dll
```

## License

- See [LICENSE](https://github.com/KMen1/Zeenox/blob/main/LICENSE)
