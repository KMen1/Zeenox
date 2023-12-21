# Zeenox

[![Discord](https://discordapp.com/api/guilds/863751874922676234/widget.png)](https://discord.gg/hGxaMkfMBR)
[![License](https://img.shields.io/github/license/kmen1/kbot)](https://github.com/KMen1/Zeenox/blob/main/LICENSE)
[![CodeFactor](https://www.codefactor.io/repository/github/kmen1/zeenox/badge)](https://www.codefactor.io/repository/github/kmen1/zeenox)
[![Deploy](https://github.com/KMen1/Zeenox/actions/workflows/deploy.yml/badge.svg)](https://github.com/KMen1/Zeenox/actions/workflows/deploy.yml)

A Discord music bot that focuses on speed and ease of usability, with a web dashboard, all for completely free. Allows playback from Spotify, YouTube, SoundCloud, BandCamp, Twitch

## Web Interface

Zeenox comes with a fully functional web interface that allows you to control the bot from your browser. It is built with Next.js and communicates with the bot via a REST API and websockets.
For more information [click here](https://github.com/KMen1/Zeenox-Web)

## Interactive Player

<p><img src="https://img001.prntscr.com/file/img001/BrF1mH45QzG8IlqoUdWuHg.png" height="400" align="right"></p>
Zeenox aims to let users have full control over the player without the need for typing commands.

- Everything important can be controlled right from the now playing message! (Back, Skip, Pause, Resume, Volume +-, Loop mode, Favorite song)
- The now playing message also shows information about the current song such as the title, length and album cover _(if available)_ as well as the volume of the player and the next 5 songs in the queue.
- The _/play_ command uses autocomplete to let you search for songs right from the chat.
- Ability to loop songs, playlists and queue.
- Ability to shuffle, clear, reverse, remove songs or duplicates from the queue.
<center><p><img src="https://img001.prntscr.com/file/img001/NoFKqOzgQIidxYS99kdq7w.png" width="400" align="center"></center>

## Action History

Zeenox keeps track of every action that is performed and allows you to view who performed it and when, if enabled.

## User Restrictions

Zeenox offers a variety of options to restrict the usage of the bot to certain users or roles.

- You can set an unlimited amount of roles that are allowed to use the bot. Users without any of these roles will not be able to use the bot.
- You can allow access to the bot on a per-user basis.
- With exclusive mode enabled, only the user that requested the currently playing song will be able to control the player.

## Channel restrictions

You can set an unlimited number of different voice channels as allowed channels. The bot will refuse to start playing in a channel that is not whitelisted.

## Built with

- [Lavalink](https://github.com/freyacodes/Lavalink)
- [Discord.NET](https://github.com/discord-net/Discord.Net)
- [Lavalink4NET](https://github.com/angelobreuer/Lavalink4NET)
- [MongoDB](https://github.com/mongodb/mongo-csharp-driver)

## License

- See [LICENSE](https://github.com/KMen1/Zeenox/blob/main/LICENSE)
