# Installation and Setup

This guide walks through installing the required drivers, setting up the app, and preparing optional media tools for streaming.

## 1. Requirements

- Windows 10 or Windows 11
- .NET Framework 4.8
- VB-Audio Virtual Cable (required)
- Optional: yt-dlp.exe for YouTube and web audio links
- Optional: ffmpeg.exe for live streams, HLS feeds, and fallback PCM streaming

## 2. Install the required virtual audio device

The app routes music and microphone input into a virtual audio source so apps such as Discord, Zoom, Teams, and OBS can pick it up as a microphone input.

Install VB-Audio Virtual Cable before running the app. This is a required component for the project to function correctly. Confirm that it appears as an output device in Windows sound settings.

The app auto-detects devices with names containing `CABLE` and shows them in the output selector.

## 3. Build the project

Open the solution in Visual Studio 2022 or another compatible .NET Framework toolchain and build it.

Typical workflow:

1. Open the MoffyVMic solution in Visual Studio
2. Restore NuGet packages
3. Choose `Debug` or `Release`
4. Build the solution

The project already references the required NAudio packages from `packages.config`.

## 4. Optional tool installation

The app can play local audio files without any extra tools. For remote audio and YouTube playback, install the following if needed:

### yt-dlp

Download `yt-dlp.exe` and place it in one of these locations:

- next to `VMic.exe`
- inside a `tools` folder next to the app
- somewhere on your `PATH`

The app automatically searches these locations.

### ffmpeg

ffmpeg is used for:

- live streams
- HLS streams
- Media Foundation fallback playback
- PCM pipe playback when a direct URL cannot be opened by Windows

Place `ffmpeg.exe` in one of the same locations above.

### JavaScript runtime for YouTube

For YouTube downloads, yt-dlp works best with a JavaScript runtime such as:

- Node.js
- Deno
- Bun

If no runtime is detected, the app warns that YouTube format discovery may be limited.

## 5. First launch

1. Start `VMic.exe`
2. Select the VB-CABLE output device
3. Choose a microphone input if you want your voice mixed in
4. Click `Start Engine`
5. Load a local folder or paste a stream URL

## 6. App data and settings

The app stores user preferences in the standard .NET Settings layer. These include:

- selected yt-dlp path
- selected browser for YouTube cookies
- selected cookies file path

## 7. Recommended setup

For the smoothest experience:

- keep yt-dlp updated with `yt-dlp -U`
- use a browser cookie file for YouTube if you hit bot protection checks
- keep ffmpeg available for live stream reliability
- use a local folder playlist for recurring sessions and background music

## 8. Common pitfalls

- VB-CABLE not detected: verify the driver is installed and the device name contains `CABLE`
- no audio in Discord/Zoom: confirm the app is sending output to the virtual cable and that the application uses the correct input device
- YouTube stream fails: install a JS runtime and use the proper cookies file or browser profile
- stream loads but no sound: verify ffmpeg is available and that the URL is a valid audio source

## 9. Troubleshooting reference

See [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for known issues and fixes.
