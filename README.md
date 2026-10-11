# MUSIC CLI

A simple local music player in the terminal written in C# / .NET 8.
Black-and-white theme, splash screen on startup, track selection with arrows.

## Commands

| Command  | What it does                                 |
| -------- | -------------------------------------------- |
| `play`   | Plays the selected track or resumes from pause |
| `pause`  | Pauses playback                              |
| `select` | Track list, select with arrows and Enter     |
| `cd`     | Change the music folder                      |
| `stop`   | Stop                                         |
| `list`   | Show the track list                          |
| `help`   | List of commands                             |
| `exit`   | Exit                                         |

Additionally:

- `play 3` — track number 3 from the list
- `play song.mp3` — by filename
- `cd D:\Music`
- `cd` without a path — will ask for a folder
- `resume`, `status`, `clear`

## Running

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd MusicCli
dotnet restore
dotnet run
Or directly with a music folder:
Bashdotnet run -- "D:\Music"
Build an exe:
Bashdotnet publish -c Release -r win-x64 --self-contained false
How it works

Spectre.Console — nice output, splash screen, list highlighting
NetCoreAudio — play / pause / resume / stop

Supported files: .mp3 .wav .ogg .flac .m4a .aac .wma
Windows
Usually works out of the box (system MCI).
Linux
For MP3 install mpg123:
Bashsudo apt install mpg123
macOS
Needs afplay (already present in the system) for wav/aiff. For mp3 it's better to install mpg123.
Controls in select

Up / Down arrows — highlight a track
Enter — select
Esc — cancel (if the terminal supports it)