# Troubleshooting

This guide covers the most common setup and runtime issues with MoffyVMic.

## 1. VB-CABLE is not detected

Symptoms:

- the output device list is empty or missing `CABLE`
- the app warns that VB-CABLE was not found

Checks:

- confirm VB-CABLE is installed and running
- open Windows Sound settings and verify the device appears
- restart the app after installing the driver

If the device still does not appear, reinstall the driver and confirm your system is using a supported Windows build.

## 2. No sound in Discord, Zoom, or Teams

Possible causes:

- the app is not running the engine
- the target app is not using VB-CABLE as its microphone input
- the music or mic channel is muted

Fix:

1. Start the engine
2. set the app input device to VB-CABLE
3. check the equalizer or mixer in the target app
4. verify the music and mic sliders are not set to zero

## 3. YouTube stream fails or returns a format error

Common signs:

- `Requested format is not available`
- `yt-dlp did not return a playable audio stream`
- no JavaScript runtime warning appears

Fix:

- install Node.js or Deno
- update yt-dlp to the latest version
- use a browser profile or cookies file if YouTube requests sign-in
- test a direct audio URL to separate URL validity from YouTube formatting issues

## 4. Cookie errors or bot checks

If YouTube asks for sign-in checks:

- use the browser profile option and close the browser completely
- or export a `cookies.txt` file for `youtube.com`
- keep the file private and not shared publicly

## 5. Live streams or HLS streams do not play

Live streams often require ffmpeg because Media Foundation cannot open them reliably.

Fix:

- install ffmpeg and ensure it is on the PATH or next to the app
- try the fallback stream path used by the app automatically
- use a direct audio URL when possible instead of a live HLS source

## 6. Playback stutters or audio glitches

Potential causes:

- low system resources
- a weak or overloaded virtual audio route
- too many audio capture devices active at once

Try:

- close unnecessary apps
- reduce music and mic volume
- keep only one active input source
- set a stable VB-CABLE output device

## 7. Local files do not appear in the playlist

Supported playlist formats are limited to common audio types.

Check:

- file extension is supported
- the folder is not empty
- the file is not corrupted
- the folder contains valid audio, not just metadata or unsupported containers

## 8. App cannot find yt-dlp or ffmpeg

The app looks for executables in the following places:

- next to `VMic.exe`
- inside a `tools` folder beside the app
- anywhere on your `PATH`

If it still cannot find them:

- point the app to the correct file using the built-in browse button
- rerun the app if the path changed while it was already open

## 9. Engine starts but no output is heard

Check these:

- VB-CABLE is selected as the active output device
- a mic or music source is enabled
- no channel is muted
- the target app is listening to the correct input device

## 10. Seeking does not work

Seeking is only available for sources that support time-based access.

- local files usually support seeking
- stream URLs may not
- live streams often have no full timeline

## Related docs

- [INSTALLATION.md](./INSTALLATION.md)
- [USAGE.md](./USAGE.md)
- [ARCHITECTURE.md](./ARCHITECTURE.md)
