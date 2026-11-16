# Usage Guide

This document explains the main actions in the MoffyVMic interface and how to use the app in everyday streaming and gaming sessions.

## Main workflow

The app works like a lightweight audio mixer:

- music plays through the selected output device
- microphone input can be mixed in
- the combined mix is sent to VB-CABLE
- other apps use VB-CABLE as a microphone input

## Start the engine

1. Open the app
2. Select a VB-CABLE output device from the output drop-down
3. Optionally select a monitor output device
4. Click `Start Engine`

Once the engine is running, the app starts sending audio into the virtual cable output.

## Local music playlist

### Load a folder

1. Click the folder picker in the playlist area
2. Choose a folder with supported audio files
3. Optionally enable recursive folder scanning
4. The app loads all supported files and builds a playlist queue

Supported file types include:

- MP3
- WAV
- FLAC
- AAC
- OGG
- M4A
- WMA
- OPUS

### Playlist controls

- `Play` starts the current track
- `Pause` mutes the current track without stopping playback
- `Stop` stops music playback
- `Previous` restarts a track or goes back in the playlist
- `Next` advances to the next item
- `Shuffle` randomizes the order of the queue
- `Repeat All` loops the playlist
- `Repeat One` repeats the current track

### Playlist behavior

- a double-click on a track starts it immediately
- pressing `Delete` removes the selected track from the queue
- the current track is highlighted in the playlist list

## Stream audio from the web

Open the `Stream` tab and paste a URL.

### Direct audio links

Direct URLs are accepted for sources like:

- MP3
- M4A
- WAV
- OGG
- AAC
- FLAC

These can often play immediately using the Windows Media Foundation pipeline.

### YouTube and web media

For YouTube or other web-hosted media, the app resolves the link using `yt-dlp`.

The app supports:

- YouTube links
- SoundCloud links
- many public media URLs exposed by yt-dlp

If playback needs more flexibility, ffmpeg is used as a fallback.

## YouTube cookies and sign-in checks

YouTube may require cookies or browser authentication on some links.

The app supports two common solutions:

1. Browser profile cookies
   - sign in to YouTube in Chrome, Edge, or Firefox
   - close the browser completely
   - select the browser in the Cookies box

2. cookies.txt file
   - export cookies for `youtube.com`
   - select the saved `cookies.txt` file from the app

The app warns you when cookies are required and can remember the chosen browser or file.

## Microphone mixing

To combine your microphone with background music:

1. Start the engine
2. Select your physical microphone input
3. Click `Enable Mic`
4. Adjust its volume using the microphone slider
5. Use `Mute Mic` if needed

The microphone signal is mixed into the same VB-CABLE output stream, so other apps hear both sources together.

## Volume and monitoring

The app includes separate controls for:

- music volume
- microphone volume
- mute toggles for each source

Use the monitor selection to optionally route the mix to another output device for monitoring while still sending the main mix to VB-CABLE.

## Seek and playback timing

- seek controls work for local files and seekable remote streams
- live or pipe-based streams may not support seeking
- the app updates the playback timer while the engine is active

## Recommended session example

A common use case:

- Start the engine
- load a folder of background music
- enable a microphone
- open Discord or Zoom
- set the input device to VB-CABLE
- use your computer audio and voice as a combined source

## Best practices

- keep the engine running during the whole session rather than starting and stopping frequently
- keep yt-dlp updated when using YouTube sources
- use the cookies feature for authenticated YouTube content
- prefer ffmpeg-capable setups for live or HLS streams
- keep your local library organized so playlist folders are easy to reload

## Related docs

- [INSTALLATION.md](./INSTALLATION.md)
- [ARCHITECTURE.md](./ARCHITECTURE.md)
- [TROUBLESHOOTING.md](./TROUBLESHOOTING.md)
