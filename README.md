# MoffyVMic

MoffyVMic (short: VMic) is a Windows desktop audio mixer by Moffy Studio that lets you combine music playback and microphone input into a virtual audio output such as VB-CABLE. It is designed for streamers, gamers, and voice-chat users who want to route background audio plus their own voice into apps like Discord, Zoom, Teams, or OBS.

The project is implemented as a .NET WinForms app using NAudio and is packaged as the `VMic` executable.

## Production

- Production: Moffy Studio
- Developer: VANG SOKCHHENG / @chhengz / @chheng_hilo

## Features

- mix local music with live microphone input
- route audio through VB-CABLE so other apps hear a combined mic feed
- load and play local audio files from folders or individual files
- build and manage playlists with shuffle, repeat, and queue controls
- stream direct audio URLs and web media using yt-dlp
- support YouTube and other web media sources with cookie handling
- fall back to ffmpeg-based PCM streaming for unsupported or live sources
- support track seeking and playback timing for seekable media
- monitor output selection and independent music/mic volume controls

## Project overview

This repository contains the source for the desktop application, the NAudio-based audio engine, device detection, playlist logic, and the media resolution layer for streaming.

## Requirements

- Windows 10 or Windows 11
- .NET Framework 4.8
- VB-Audio Virtual Cable (required) installed and available as an output device
- Optional: yt-dlp.exe for YouTube and other web sources
- Optional: ffmpeg.exe for live/HLS fallback playback
- Optional: Node.js, Deno, or Bun for improved YouTube format discovery

Important: MoffyVMic depends on VB-Audio Virtual Cable to send the mixed audio output to other apps as a virtual microphone input. Install it before using the app.

## Quick start

1. Install VB-Audio Virtual Cable
2. Open the MoffyVMic solution in Visual Studio
3. Restore NuGet packages
4. Build the solution
5. Run `VMic.exe`
6. Select the VB-CABLE output device
7. Optionally enable your microphone
8. Load a folder or paste a stream URL

## Main usage flow

### Local audio playback

- load a folder of compatible audio files
- play, pause, stop, seek, and skip tracks
- use repeat or shuffle modes for playback flow

### Microphone mixing

- choose the microphone device
- click `Enable Mic`
- adjust microphone level with the slider
- mute or disable the mic as needed

### Stream playback

- open the stream input
- paste a direct audio URL or YouTube link
- the app resolves the link using yt-dlp
- playback is mixed into the same VB-CABLE output stream

## Optional external tools

### yt-dlp

Used to resolve YouTube and web media links to playable audio streams.

Search locations:

- next to the executable
- in a `tools` subfolder next to the app
- on your system `PATH`

### ffmpeg

Used for live streams, HLS streams, and PCM fallback playback when Media Foundation cannot open the stream.

### JavaScript runtime

For YouTube links, installing Node.js or Deno helps yt-dlp discover better formats and avoids reduced-format playback errors.

## Release packaging

The project includes a single-installer packaging flow for a release build.

- Run `build-release.ps1` to build the Release configuration and prepare a bundled app package
- If Inno Setup is installed, the script also compiles an all-in-one Windows installer from `installer/MoffyVMic.iss`
- The installer includes the app, bundled runtime dependencies, and any optional files placed in a `tools` folder
- Before launching the app, users should install VB-Audio Virtual Cable. A helper script, `check-prerequisites.ps1`, validates this requirement and can open the VB-Audio download page if needed

## Documentation

See the docs folder for full project references:

- [docs/INSTALLATION.md](./docs/INSTALLATION.md)
- [docs/USAGE.md](./docs/USAGE.md)
- [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)
- [docs/TROUBLESHOOTING.md](./docs/TROUBLESHOOTING.md)

## Project structure

- `Core/AudioEngine.cs` – audio mixing and playback engine
- `Core/DeviceManager.cs` – device enumeration and VB-CABLE detection
- `Core/PlaylistManager.cs` – queueing, repeat, shuffle, and playlist state
- `Core/YouTubeAudioService.cs` – yt-dlp media resolution and URL handling
- `Core/ProcessAudioStream.cs` – ffmpeg-based fallback stream wrapper
- `MainForm.cs` – app logic and event handlers
- `MainForm.Layout.cs` – UI layout code
- `Properties/` – settings and resources

## License

This project is distributed under the terms of the license in [LICENSE.txt](./LICENSE.txt).

