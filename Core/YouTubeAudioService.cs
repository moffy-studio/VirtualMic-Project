using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualMicMixer.Core
{
    /// <summary>Metadata + direct media URL resolved by yt-dlp for one link.</summary>
    public class StreamInfo
    {
        public string Title { get; set; }
        public string StreamUrl { get; set; }
        public bool IsLive { get; set; }
        public double DurationSeconds { get; set; }

        /// <summary>Non-fatal problem worth showing to the user (e.g. no JS runtime).</summary>
        public string Warning { get; set; }
    }

    /// <summary>
    /// Resolves web media links (YouTube, SoundCloud, …) to a direct audio URL with
    /// yt-dlp, and hosts the ffmpeg PCM fallback used when the direct URL cannot be
    /// opened by Media Foundation (live streams, HLS, unsupported containers).
    ///
    /// External tools are optional and only needed for links that are not already
    /// direct audio files.  They are looked up in the app folder, a "tools"
    /// sub-folder and finally on PATH; the user can override the yt-dlp location.
    /// </summary>
    public static class YouTubeAudioService
    {
        private static readonly string[] AudioExtensions =
            { ".mp3", ".m4a", ".aac", ".wav", ".ogg", ".opus", ".flac", ".wma" };

        private static readonly string[] YouTubeHosts =
            { "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtu.be", "youtube-nocookie.com" };

        public static readonly string[] SupportedCookieBrowsers =
            { "chrome", "edge", "firefox", "brave", "opera", "vivaldi", "chromium" };

        // ══ TOOL DISCOVERY ═══════════════════════════════════════════════════
        public static string FindYtDlp(string configuredPath)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
                return configuredPath;
            return FindExecutable("yt-dlp.exe");
        }

        public static string FindFfmpeg() => FindExecutable("ffmpeg.exe");

        private static string FindExecutable(string fileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, fileName),
                Path.Combine(baseDir, "tools", fileName)
            };

            foreach (var candidate in candidates)
                if (File.Exists(candidate)) return candidate;

            string pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathVar.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(dir.Trim(), fileName);
                    if (File.Exists(candidate)) return candidate;
                }
                catch (ArgumentException) { }
            }
            return null;
        }

        // ══ URL CLASSIFICATION ═══════════════════════════════════════════════
        public static bool IsYouTubeUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return false;
            string host = uri.Host.ToLowerInvariant();
            return YouTubeHosts.Any(h => host == h || host.EndsWith("." + h, StringComparison.Ordinal));
        }

        /// <summary>True for links that can be handed straight to Media Foundation.</summary>
        public static bool IsDirectAudioUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
            if (IsYouTubeUrl(url)) return false;

            string ext = Path.GetExtension(uri.AbsolutePath);
            return !string.IsNullOrEmpty(ext) &&
                   AudioExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        // ══ RESOLVE ══════════════════════════════════════════════════════════
        /// <summary>
        /// Asks yt-dlp for the track title and the direct audio URL of a link.
        /// Throws <see cref="InvalidOperationException"/> with a user-readable
        /// message when yt-dlp is missing or the link cannot be extracted.
        /// </summary>
        public static async Task<StreamInfo> ResolveAsync(
            string url, string ytDlpPath, string cookieBrowser, string cookiesFile,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ytDlpPath))
                throw new InvalidOperationException(
                    "yt-dlp was not found.\r\n\r\nDownload yt-dlp.exe and place it next to " +
                    "VirtualMicMixer.exe, or use the “…” button to point to it.");

            if (!string.IsNullOrWhiteSpace(cookiesFile) && !File.Exists(cookiesFile))
                throw new InvalidOperationException(
                    "Cookies file not found:\r\n" + cookiesFile +
                    "\r\n\r\nClick the 📄 button to choose it again, or ✕ to clear it.");

            // yt-dlp only enables Deno by default. Without a JS runtime YouTube
            // exposes a reduced format list, which makes "bestaudio" fail with
            // "Requested format is not available".
            var runtimeArgs = BuildJsRuntimeArguments().ToList();

            // Progressively broader format choices; only format errors are retried.
            string[] formatSelectors = { "bestaudio[ext=m4a]/bestaudio/best", "ba/b", "best" };

            ProcessResult result = await RunFormatsAsync(runtimeArgs).ConfigureAwait(false);

            // Older yt-dlp builds do not know --js-runtimes; retry without it.
            if (result.ExitCode != 0 && runtimeArgs.Count > 0 && IsUnknownOptionError(result.StandardError))
                result = await RunFormatsAsync(new List<string>()).ConfigureAwait(false);

            if (result.ExitCode != 0)
                throw new InvalidOperationException(BuildYtDlpError(result.StandardError));

            async Task<ProcessResult> RunFormatsAsync(List<string> jsRuntimeArgs)
            {
                ProcessResult last = default(ProcessResult);
                foreach (string format in formatSelectors)
                {
                    var args = new StringBuilder("--no-playlist --no-progress ");
                    if (!string.IsNullOrWhiteSpace(cookiesFile))
                        args.Append("--cookies ").Append(QuoteArgument(cookiesFile)).Append(' ');
                    else if (!string.IsNullOrWhiteSpace(cookieBrowser))
                        args.Append("--cookies-from-browser ").Append(QuoteArgument(cookieBrowser)).Append(' ');

                    foreach (string runtimeArg in jsRuntimeArgs)
                        args.Append(runtimeArg).Append(' ');

                    args.Append("-f ").Append(QuoteArgument(format)).Append(' ');
                    args.Append("--print \"VMTITLE:%(title)s\" ");
                    args.Append("--print \"VMLIVE:%(is_live)s\" ");
                    args.Append("--print \"VMDUR:%(duration)s\" ");
                    args.Append("-g ");
                    args.Append(QuoteArgument(url));

                    var psi = new ProcessStartInfo
                    {
                        FileName = ytDlpPath,
                        Arguments = args.ToString(),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    last = await RunCaptureAsync(psi, cancellationToken).ConfigureAwait(false);
                    if (last.ExitCode == 0) break;
                    if (!IsFormatSelectionError(last.StandardError)) break;
                }
                return last;
            }

            string title = null, live = null, duration = null, streamUrl = null;
            foreach (string rawLine in result.StandardOutput.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                if (line.StartsWith("VMTITLE:", StringComparison.Ordinal)) title = line.Substring(8).Trim();
                else if (line.StartsWith("VMLIVE:", StringComparison.Ordinal)) live = line.Substring(7).Trim();
                else if (line.StartsWith("VMDUR:", StringComparison.Ordinal)) duration = line.Substring(6).Trim();
                else if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    streamUrl = line.Trim();
            }

            if (string.IsNullOrWhiteSpace(streamUrl))
                throw new InvalidOperationException(
                    "yt-dlp did not return a playable audio stream for this link." +
                    Environment.NewLine + Environment.NewLine + BuildYtDlpError(result.StandardError));

            double seconds;
            if (!double.TryParse(duration, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
                seconds = 0;

            string warning = null;
            if (runtimeArgs.Count == 0)
            {
                warning = "No JavaScript runtime (Deno or Node.js) found – YouTube may hide audio formats. " +
                          "Install Node.js or Deno to fix this.";
            }
            else if ((result.StandardError ?? string.Empty)
                     .IndexOf("No supported JavaScript runtime", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                warning = "yt-dlp could not load the JavaScript runtime – YouTube may hide audio formats.";
            }

            return new StreamInfo
            {
                Title = string.IsNullOrWhiteSpace(title) ? "YouTube stream" : title,
                StreamUrl = streamUrl,
                IsLive = string.Equals(live, "True", StringComparison.OrdinalIgnoreCase),
                DurationSeconds = seconds,
                Warning = warning
            };
        }

        /// <summary>
        /// Checks that a file looks like a Netscape-format cookies.txt export
        /// (the format produced by browser cookie-export extensions).
        /// </summary>
        public static bool LooksLikeNetscapeCookies(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;

                using (var reader = new StreamReader(path))
                {
                    string line;
                    int dataLines = 0;
                    while ((line = reader.ReadLine()) != null && dataLines < 200)
                    {
                        if (line.StartsWith("#", StringComparison.Ordinal))
                        {
                            if (line.IndexOf("Netscape HTTP Cookie File", StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                            continue;
                        }
                        if (line.Trim().Length == 0) continue;

                        dataLines++;
                        // domain \t includeSubdomains \t path \t secure \t expiry \t name \t value
                        if (line.Split('\t').Length >= 7) return true;
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return false;
        }

        // ══ FALLBACK STREAM ══════════════════════════════════════════════════
        /// <summary>
        /// Starts ffmpeg decoding the given media URL into raw 48 kHz stereo PCM.
        /// Used when Media Foundation cannot open the URL directly.
        /// </summary>
        public static ProcessAudioStream StartPcmStream(string ffmpegPath, string streamUrl)
        {
            if (string.IsNullOrWhiteSpace(ffmpegPath))
                throw new InvalidOperationException(
                    "ffmpeg was not found.\r\n\r\nThis link needs ffmpeg for playback. " +
                    "Download ffmpeg.exe and place it next to VirtualMicMixer.exe (or on PATH).");

            return new ProcessAudioStream(ffmpegPath, streamUrl);
        }

        // ══ PROCESS HELPERS ══════════════════════════════════════════════════

        /// <summary>
        /// Enables every JavaScript runtime we can find.  YouTube needs one to
        /// expose the full format list; yt-dlp only enables Deno by default.
        /// </summary>
        private static IEnumerable<string> BuildJsRuntimeArguments()
        {
            var runtimes = new[]
            {
                new { Name = "deno", Exe = "deno.exe" },
                new { Name = "node", Exe = "node.exe" },
                new { Name = "bun", Exe = "bun.exe" }
            };

            foreach (var runtime in runtimes)
            {
                string path = FindExecutable(runtime.Exe);
                if (path != null)
                    yield return "--js-runtimes " + QuoteArgument(runtime.Name + ":" + path);
            }
        }

        private static bool IsFormatSelectionError(string stderr)
        {
            string text = stderr ?? string.Empty;
            return text.IndexOf("Requested format is not available", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("No video formats found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("Only images are available", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsUnknownOptionError(string stderr)
        {
            string text = (stderr ?? string.Empty).ToLowerInvariant();
            return text.Contains("no such option") ||
                   text.Contains("unrecognized argument") ||
                   text.Contains("unrecognized option");
        }
        private static Task<ProcessResult> RunCaptureAsync(ProcessStartInfo psi, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using (var process = new Process { StartInfo = psi })
                {
                    var stdout = new StringBuilder();
                    var stderr = new StringBuilder();

                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null) lock (stdout) stdout.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null) lock (stderr) stderr.AppendLine(e.Data);
                    };

                    if (!process.Start())
                        throw new InvalidOperationException("Could not start " + Path.GetFileName(psi.FileName) + ".");

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    while (!process.WaitForExit(100))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            try { process.Kill(); } catch (InvalidOperationException) { }
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    process.WaitForExit(); // ensure async output handlers have drained

                    string outText, errText;
                    lock (stdout) outText = stdout.ToString();
                    lock (stderr) errText = stderr.ToString();
                    return new ProcessResult(process.ExitCode, outText, errText);
                }
            }, cancellationToken);
        }

        private static string BuildYtDlpError(string stderr)
        {
            var lines = (stderr ?? string.Empty)
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            string message = lines.FirstOrDefault(l => l.StartsWith("ERROR:", StringComparison.Ordinal))
                             ?? lines.LastOrDefault()
                             ?? "Unknown yt-dlp error.";

            if (message.StartsWith("ERROR:", StringComparison.Ordinal))
                message = message.Substring(6).Trim();

            if (message.IndexOf("Sign in to confirm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                message += Environment.NewLine + Environment.NewLine +
                    "YouTube is asking for a sign-in check. Open YouTube in your browser and log in, " +
                    "then pick that browser in the “Cookies” box. Keeping yt-dlp up to date also helps " +
                    "(run: yt-dlp -U).";
            }
            else if (message.IndexOf("cookie database", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("Failed to decrypt", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                message += Environment.NewLine + Environment.NewLine +
                    "The browser is still running and is locking its cookie store. Close the browser " +
                    "completely (also from the system tray) and click Stream again.";
            }
            else if (message.IndexOf("Requested format is not available", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     message.IndexOf("No video formats found", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                message += Environment.NewLine + Environment.NewLine +
                    "YouTube did not expose a usable audio format. yt-dlp needs a JavaScript runtime " +
                    "(Deno or Node.js) for the full YouTube format list — install one and restart the app. " +
                    "Updating yt-dlp (yt-dlp -U) also helps.";
            }
            return message;
        }

        private static string QuoteArgument(string value)
        {
            string escaped = (value ?? string.Empty).Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        private struct ProcessResult
        {
            public int ExitCode { get; }
            public string StandardOutput { get; }
            public string StandardError { get; }

            public ProcessResult(int exitCode, string stdout, string stderr)
            {
                ExitCode = exitCode;
                StandardOutput = stdout;
                StandardError = stderr;
            }
        }
    }
}
