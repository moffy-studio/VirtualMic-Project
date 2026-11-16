## 🌐 YouTube / URL streaming

Open the **🌐 Stream** tab, paste a link and press **Stream** (or Enter). The **📂 Local Playlist**
tab holds the folder playlist; both tabs share the same transport controls, volume and mix, so
Discord/Zoom hear everything through VB-CABLE.

| Link | Needs |
|------|-------|
| Direct audio URL (`.mp3`, `.m4a`, `.wav`, `.ogg`, …) | nothing |
| YouTube / SoundCloud / other web media | `yt-dlp.exe` |
| YouTube (full format list) | also **Deno or Node.js** (yt-dlp’s JavaScript runtime) |
| Live streams and HLS | `yt-dlp.exe` + `ffmpeg.exe` |

The app passes any JavaScript runtime it finds (`deno.exe`, `node.exe`, `bun.exe`) to yt-dlp as
`--js-runtimes`. Without one, YouTube exposes only a reduced format list and playback can fail with
*“Requested format is not available”*.

**How a link is played**

1. yt-dlp resolves the link to a direct audio URL and the track title.
2. The URL is streamed through Media Foundation, which keeps seeking and duration working.
3. If Media Foundation cannot open it (live/HLS/WebM), the app falls back to an ffmpeg PCM pipe —
   instant playback without seeking.

**Finding the tools** — yt-dlp and ffmpeg are looked up next to `VirtualMicMixer.exe`, in a
`tools\` sub-folder of the app, and on `PATH`. Use the **…** button to point at a specific
`yt-dlp.exe`; the path is remembered.

**“Sign in to confirm you're not a bot”** — YouTube wants your YouTube login cookies. Two ways:

1. **Browser profile** — log in to YouTube in Chrome, Edge or Firefox, pick that browser in the
   **Cookies** box, then close the browser completely (Chrome/Edge lock their cookie database while
   running, see [yt-dlp#7271](https://github.com/yt-dlp/yt-dlp/issues/7271)) and click Stream again.
2. **cookies.txt file** — export cookies for `youtube.com` with a cookie-export extension such as
   *Get cookies.txt LOCALLY*, then click **📄** in the Stream tab and pick the file. This works while
   the browser stays open. The chosen file is remembered; **✕** clears it.

The **How do I get YouTube cookies?** link in the Stream tab shows these steps too. Keep the file
private — it grants access to your YouTube session. Keeping yt-dlp updated (`yt-dlp -U`) also helps.

Only stream content you have the rights to.

## 👨‍💻 Developer

- VANG SOKCHHENG / **@chhengz** / **@chheng_hilo**