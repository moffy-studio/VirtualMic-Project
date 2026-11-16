# Architecture Overview

This project is a Windows Forms desktop application built around NAudio and a small audio-engine abstraction layer.

## High-level design

The app contains three major parts:

1. UI layer (`MainForm.cs`, `MainForm.Layout.cs`)
2. Audio engine (`Core/AudioEngine.cs`)
3. Streaming and playlist services (`Core/*.cs`)

## UI layer

The WinForms interface provides:

- VB-CABLE device selection
- microphone input selection
- playlist loading and playback controls
- URL streaming controls
- YouTube cookie configuration
- volume and mute controls
- seek and position display

The UI is built primarily in code-behind with `MainForm.Layout.cs` and `MainForm.cs` rather than a separate designer-based layout system.

## Audio engine

`AudioEngine` owns the main signal path:

- output to VB-CABLE using `WaveOutEvent`
- a `MixingSampleProvider` that combines the music and microphone sources
- a `MixerChannel` for each active source
- `WaveInEvent` capture for the microphone input
- end-of-stream detection for music playback

The engine is responsible for:

- starting and stopping the VB-CABLE output
- loading local files
- loading remote URLs through Media Foundation
- loading ffmpeg PCM pipe streams
- mixing microphone input and playback content
- raising playback completion notifications safely

## Mixer model

Each source is wrapped in a `MixerChannel`:

- music channel for local files or stream playback
- microphone channel for live input capture
- individual volume and mute state per source

This enables independent control of the playback and mic channels while the main audio mix is rendered to VB-CABLE.

## Playlist system

`PlaylistManager` handles:

- loading folders with supported audio extensions
- recursive scanning options
- shuffle and repeat modes
- queue ordering and current-track tracking
- next/previous navigation
- track removal

Queue ordering is stored as a master list plus a current runtime queue, which makes shuffle and repeat features easy to control without altering the underlying folder scan results.

## Streaming system

`YouTubeAudioService` is responsible for media resolution and stream startup.

It does the following:

- checks for `yt-dlp.exe` and `ffmpeg.exe`
- detects direct audio URLs vs. YouTube/web links
- resolves YouTube links to direct audio URLs
- passes cookie data for signed-in media access
- chooses audio format combinations suitable for yt-dlp
- constructs fallback `ffmpeg` PCM pipelines for unsupported or live streams

## ProcessAudioStream

`ProcessAudioStream` wraps ffmpeg as a live audio source.

The class:

- starts a child ffmpeg process
- captures encoded output
- converts to PCM for the NAudio pipeline
- buffers data until the app is ready to consume it
- surfaces errors and end-of-stream state

This is used when the direct secure stream is not seekable or cannot be opened through Media Foundation.

## Device discovery

`DeviceManager` inspects Windows audio devices and identifies candidates that look like VB-CABLE.

It uses:

- `WaveOut.DeviceCount` for output devices
- `WaveIn.DeviceCount` for microphone-like input devices
- device naming heuristics to highlight `CABLE` devices

## Threading and safety

A notable part of the project is the audio-thread safety around end-of-track detection.

The app avoids directly manipulating the mixer from inside NAudio callbacks. Instead:

- end-of-stream detection is triggered in a safe layer
- notifications are marshaled to the UI thread
- playback changes happen after the audio callback exits cleanly

This prevents deadlocks while keeping track changes and playlist transitions stable.

## Project structure

- `MainForm.cs` – main event logic
- `MainForm.Layout.cs` – generated UI layout code
- `Core/AudioEngine.cs` – audio routing and mixer logic
- `Core/DeviceManager.cs` – device enumeration
- `Core/PlaylistManager.cs` – playlist queue behavior
- `Core/YouTubeAudioService.cs` – yt-dlp and stream resolution
- `Core/ProcessAudioStream.cs` – ffmpeg PCM bridge
- `Properties/` – settings and resources

## Dependencies

Core dependencies used by the app include:

- NAudio
- .NET WinForms
- Windows audio APIs
- yt-dlp for remote web media
- ffmpeg for fallback/live stream handling

## Related docs

- [INSTALLATION.md](./INSTALLATION.md)
- [USAGE.md](./USAGE.md)
- [TROUBLESHOOTING.md](./TROUBLESHOOTING.md)
