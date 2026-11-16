using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirtualMicMixer.Core;
using VirtualMicMixer.Properties;

namespace VirtualMicMixer
{
    public partial class MainForm : Form
    {
        // ── Core objects ──────────────────────────────────────────────────────
        private readonly AudioEngine _engine = new AudioEngine();
        private readonly PlaylistManager _playlist = new PlaylistManager();

        // ── Device lists ──────────────────────────────────────────────────────
        private List<AudioDeviceInfo> _outputs;
        private List<AudioDeviceInfo> _inputs;

        // ── State flags ───────────────────────────────────────────────────────
        private bool _engineRunning;
        private bool _musicPaused;
        private bool _playingStream;
        private bool _streamBusy;
        private bool _uiReady;
        private bool _closing;
        private string _streamTitle;

        // ── Streaming ─────────────────────────────────────────────────────────
        private string _ytDlpPath;
        private string _cookiesFile;
        private CancellationTokenSource _streamCts;
        private readonly ToolTip _toolTip = new ToolTip();

        // ── Colors for toggle-button active state ─────────────────────────────
        private static readonly Color ClrToggleOn = Color.FromArgb(0, 110, 180);
        private static readonly Color ClrToggleOff = Color.FromArgb(58, 58, 64);

        // ─────────────────────────────────────────────────────────────────────
        public MainForm()
        {
            InitializeComponent();
            BuildLayout();   // code-built UI lives in MainForm.Layout.cs
            LoadDevices();
            LoadStreamSettings();
            UpdateUI();
            RefreshPlaylistView();

            this.FormClosing += (s, e) =>
            {
                _closing = true;
                _streamCts?.Cancel();
                _engine.Dispose();
            };
            _engine.MusicFinished += Engine_MusicFinished;
        }

        // ══ DEVICE ENUMERATION ═══════════════════════════════════════════════
        private void LoadDevices()
        {
            _outputs = DeviceManager.GetOutputDevices();
            _inputs = DeviceManager.GetInputDevices();

            cmbVbCable.Items.Clear();
            cmbMonitor.Items.Clear();
            cmbMicInput.Items.Clear();
            cmbMonitor.Items.Add("(none)");

            foreach (var d in _outputs)
            {
                string label = d.IsVbCable ? $"★ {d.Name}" : d.Name;
                cmbVbCable.Items.Add(label);
                cmbMonitor.Items.Add(label);
            }
            foreach (var d in _inputs)
                cmbMicInput.Items.Add(d.Name);

            int vbIdx = DeviceManager.FindVbCableOutputIndex();
            cmbVbCable.SelectedIndex = vbIdx >= 0 ? vbIdx : (cmbVbCable.Items.Count > 0 ? 0 : -1);
            cmbMonitor.SelectedIndex = 0;
            if (cmbMicInput.Items.Count > 0) cmbMicInput.SelectedIndex = 0;

            if (vbIdx < 0)
                SetStatus("⚠  VB-CABLE not detected. Install VB-CABLE and restart.", Color.Orange);
        }

        // ══ ENGINE ════════════════════════════════════════════════════════════
        private void BtnInitEngine_Click(object sender, EventArgs e)
        {
            if (_engineRunning)
            {
                _streamCts?.Cancel();
                _engine.Stop();
                _engineRunning = false;
                _playingStream = false;
                _musicPaused = false;
                tmrPosition.Stop();
                btnInitEngine.Text = "▶  Start Engine";
                btnInitEngine.BackColor = Color.FromArgb(0, 120, 60);
                lblEngineStatus.Text = "Engine stopped";
                lblEngineStatus.ForeColor = Color.FromArgb(160, 160, 160);
                SetStatus("Engine stopped.");
                UpdateUI();
                return;
            }

            if (cmbVbCable.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a VB-CABLE output device.", "No device",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int vbIdx = cmbVbCable.SelectedIndex;
            int monIdx = cmbMonitor.SelectedIndex <= 0 ? -1 : cmbMonitor.SelectedIndex - 1;

            try
            {
                _engine.Initialise(vbIdx, monIdx);
                _engineRunning = true;
                tmrPosition.Start();
                btnInitEngine.Text = "⏹  Stop Engine";
                btnInitEngine.BackColor = Color.FromArgb(140, 40, 40);
                lblEngineStatus.Text = "● Running";
                lblEngineStatus.ForeColor = Color.FromArgb(80, 220, 100);
                SetStatus($"Engine running → {_outputs[vbIdx].Name}");
                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start engine:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══ STREAMING (YouTube / direct URL) ══════════════════════════════════
        private void LoadStreamSettings()
        {
            _ytDlpPath = YouTubeAudioService.FindYtDlp(Settings.Default.YtDlpPath);

            string browser = Settings.Default.CookieBrowser;
            int index = string.IsNullOrEmpty(browser) ? -1 : cmbCookies.Items.IndexOf(browser);
            cmbCookies.SelectedIndex = index > 0 ? index : 0;

            _cookiesFile = Settings.Default.CookiesFile;

            UpdateYtDlpStatus();
            UpdateCookieState();
            _uiReady = true;
        }

        private void UpdateYtDlpStatus()
        {
            _ytDlpPath = YouTubeAudioService.FindYtDlp(Settings.Default.YtDlpPath);

            bool found = _ytDlpPath != null;
            lblYtDlpStatus.Text = found ? "yt-dlp: ready" : "yt-dlp: missing";
            lblYtDlpStatus.ForeColor = found
                ? Color.FromArgb(110, 200, 120)
                : Color.FromArgb(235, 145, 90);
            _toolTip.SetToolTip(lblYtDlpStatus,
                found ? _ytDlpPath : "yt-dlp.exe was not found. Click “…” to locate it.");
        }

        private void BtnBrowseYtDlp_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Locate yt-dlp.exe";
                dlg.Filter = "yt-dlp|yt-dlp.exe|Executables|*.exe";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                Settings.Default.YtDlpPath = dlg.FileName;
                Settings.Default.Save();
                UpdateYtDlpStatus();
                SetStatus($"yt-dlp set to: {dlg.FileName}");
            }
        }

        private void CmbCookies_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_uiReady) return;
            Settings.Default.CookieBrowser = SelectedCookieBrowser ?? string.Empty;
            Settings.Default.Save();
            UpdateCookieState();
        }

        private void BtnCookiesFile_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select a cookies.txt exported from your browser";
                dlg.Filter = "Cookies file (Netscape format)|*.txt|All files|*.*";
                if (!string.IsNullOrWhiteSpace(_cookiesFile) && File.Exists(_cookiesFile))
                    dlg.FileName = _cookiesFile;

                if (dlg.ShowDialog() != DialogResult.OK) return;

                if (!YouTubeAudioService.LooksLikeNetscapeCookies(dlg.FileName))
                {
                    DialogResult answer = MessageBox.Show(
                        "This file does not look like a Netscape cookies.txt export.\r\n\r\nUse it anyway?",
                        "Cookies file", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes) return;
                }

                Settings.Default.CookiesFile = dlg.FileName;
                Settings.Default.Save();
                _cookiesFile = dlg.FileName;
                UpdateCookieState();
                SetStatus("Cookies file loaded: " + Path.GetFileName(dlg.FileName));
            }
        }

        private void BtnCookiesClear_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_cookiesFile)) return;

            Settings.Default.CookiesFile = string.Empty;
            Settings.Default.Save();
            _cookiesFile = null;
            UpdateCookieState();
            SetStatus("Cookies file cleared.");
        }

        private void LnkCookieHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(
                "Option 1 – read cookies from your browser\r\n" +
                "1. Log in to YouTube in Chrome, Edge or Firefox.\r\n" +
                "2. Close that browser completely (also from the system tray).\r\n" +
                "3. Select the browser in the Cookies box.\r\n\r\n" +
                "Option 2 – use a cookies.txt file (browser can stay open)\r\n" +
                "1. Install a cookie-export extension, e.g. “Get cookies.txt LOCALLY”.\r\n" +
                "2. Open youtube.com, then export the cookies for youtube.com.\r\n" +
                "3. Click the 📄 button and select the .txt file.\r\n\r\n" +
                "Keep the file private – it grants access to your YouTube session.",
                "How to get YouTube cookies",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateCookieState()
        {
            btnCookiesClear.Enabled = !string.IsNullOrWhiteSpace(_cookiesFile);

            if (!string.IsNullOrWhiteSpace(_cookiesFile))
            {
                bool exists = File.Exists(_cookiesFile);
                lblCookieState.Text = exists
                    ? "Cookies file: " + Path.GetFileName(_cookiesFile)
                    : "Cookies file not found: " + _cookiesFile;
                lblCookieState.ForeColor = exists
                    ? Color.FromArgb(110, 200, 120)
                    : Color.FromArgb(235, 145, 90);
                _toolTip.SetToolTip(lblCookieState, _cookiesFile);
                _toolTip.SetToolTip(cmbCookies, "Ignored while a cookies.txt file is loaded");
                return;
            }

            string browser = SelectedCookieBrowser;
            if (!string.IsNullOrEmpty(browser))
            {
                lblCookieState.Text = $"Cookies: browser ({browser}) – close it completely before streaming";
                lblCookieState.ForeColor = Color.FromArgb(200, 200, 120);
            }
            else
            {
                lblCookieState.Text = "Cookies: none – YouTube may ask for a sign-in check";
                lblCookieState.ForeColor = Color.FromArgb(150, 150, 150);
            }
            _toolTip.SetToolTip(lblCookieState, string.Empty);
            _toolTip.SetToolTip(cmbCookies, "Read YouTube cookies from this browser's profile");
        }

        private string SelectedCookieBrowser =>
            cmbCookies.SelectedIndex > 0 ? cmbCookies.SelectedItem as string : null;

        private async void BtnStream_Click(object sender, EventArgs e)
        {
            if (_streamBusy) return;

            string url = txtStreamUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                SetStatus("⚠  Paste a YouTube link or audio URL first.", Color.Orange);
                return;
            }
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                SetStatus("⚠  That does not look like a valid link.", Color.Orange);
                return;
            }
            if (!_engineRunning)
            {
                SetStatus("⚠  Start the engine first.", Color.Orange);
                return;
            }

            _streamCts?.Cancel();
            _streamCts?.Dispose();
            _streamCts = new CancellationTokenSource();
            CancellationToken ct = _streamCts.Token;

            SetStreamBusy(true);
            try
            {
                _engine.StopMusic();
                _musicPaused = false;
                _playingStream = false;

                string title;
                string streamWarning = null;
                bool loaded;

                if (YouTubeAudioService.IsDirectAudioUrl(url))
                {
                    title = Path.GetFileNameWithoutExtension(new Uri(url).AbsolutePath);
                    SetStatus("⏳ Opening audio stream…");
                    loaded = await TryLoadUrlStreamAsync(url, ct);
                    if (!loaded)
                        loaded = await StartPcmFallbackAsync(url, ct);
                }
                else
                {
                    SetStatus("🔎 Resolving link with yt-dlp…");
                    StreamInfo info = await YouTubeAudioService.ResolveAsync(
                        url, _ytDlpPath, SelectedCookieBrowser, _cookiesFile, ct);

                    ct.ThrowIfCancellationRequested();

                    title = info.Title;
                    streamWarning = info.Warning;
                    loaded = !info.IsLive && await TryLoadUrlStreamAsync(info.StreamUrl, ct);
                    if (!loaded)
                        loaded = await StartPcmFallbackAsync(info.StreamUrl, ct);
                }

                if (_closing || IsDisposed)
                {
                    _engine.StopMusic();
                    return;
                }

                if (!loaded)
                    throw new InvalidOperationException("The audio stream could not be opened.");

                _playingStream = true;
                _streamTitle = title;
                _engine.MusicVolume = trkMusicVol.Value / 100f;

                trkSeek.Value = 0;
                trkSeek.Maximum = Math.Max(1, (int)_engine.MusicDuration.TotalSeconds);
                lblNowPlaying.Text = "🌐  " + title;
                lblMusicPos.Text = "0:00";
                lblMusicDur.Text = _engine.MusicDuration.TotalSeconds > 0
                    ? $"/ {FormatTime(_engine.MusicDuration)}"
                    : "/ live";

                SetStatus($"▶  Streaming: {title}");
                if (!string.IsNullOrEmpty(streamWarning))
                    SetStatus("⚠  " + streamWarning, Color.Orange);
                UpdateInfoPanel();
            }
            catch (OperationCanceledException)
            {
                _engine.StopMusic();
                if (!_closing && !IsDisposed) SetStatus("Stream cancelled.");
            }
            catch (Exception ex)
            {
                _engine.StopMusic();
                _playingStream = false;
                if (_closing || IsDisposed) return;

                lblNowPlaying.Text = "— No track selected —";
                SetStatus("⚠  Cannot stream: " + FirstLine(ex.Message), Color.Orange);
                MessageBox.Show(ex.Message, "Streaming Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (!_closing && !IsDisposed)
                {
                    SetStreamBusy(false);
                    UpdateUI();
                }
            }
        }

        /// <summary>Loads a seekable remote URL via Media Foundation. Returns false on failure.</summary>
        private async Task<bool> TryLoadUrlStreamAsync(string streamUrl, CancellationToken ct)
        {
            try
            {
                await Task.Run(() => _engine.LoadMusicStream(streamUrl), ct);
                return _engine.IsMusicLoaded;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// Starts the ffmpeg PCM pipe (live streams / containers Media Foundation
        /// cannot open) and waits until audio is actually flowing or ffmpeg failed.
        /// </summary>
        private async Task<bool> StartPcmFallbackAsync(string streamUrl, CancellationToken ct)
        {
            ProcessAudioStream session = null;
            try
            {
                SetStatus("⏳ Opening live audio stream…");
                string ffmpeg = YouTubeAudioService.FindFfmpeg();
                session = await Task.Run(
                    () => YouTubeAudioService.StartPcmStream(ffmpeg, streamUrl), ct);

                for (int i = 0; i < 50 && !ct.IsCancellationRequested; i++)
                {
                    if (session.BufferedBytes > 0) break;
                    if (session.Ended) break;
                    await Task.Delay(200, ct);
                }

                if (session.Ended)
                {
                    string detail = session.ErrorText;
                    session.Dispose();
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(detail)
                            ? "The stream ended before any audio was received."
                            : detail);
                }

                _engine.LoadMusic(session);
                return _engine.IsMusicLoaded;
            }
            catch
            {
                session?.Dispose();
                throw;
            }
        }

        private void TxtStreamUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || _streamBusy) return;
            e.SuppressKeyPress = true;
            BtnStream_Click(sender, e);
        }

        private void SetStreamBusy(bool busy)
        {
            _streamBusy = busy;
            btnStream.Text = busy ? "⏳  Loading…" : "▶  Stream";
            txtStreamUrl.Enabled = !busy;
            btnBrowseYtDlp.Enabled = !busy;
            cmbCookies.Enabled = !busy;
            btnCookiesFile.Enabled = !busy;
            btnCookiesClear.Enabled = !busy && !string.IsNullOrWhiteSpace(_cookiesFile);
            btnStream.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        // ══ PLAYLIST – FOLDER LOADING ═════════════════════════════════════════
        private void BtnBrowseFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select a folder containing audio files";
                dlg.ShowNewFolderButton = false;

                if (!string.IsNullOrEmpty(txtFolderPath.Text) &&
                    Directory.Exists(txtFolderPath.Text))
                    dlg.SelectedPath = txtFolderPath.Text;

                if (dlg.ShowDialog() != DialogResult.OK) return;

                txtFolderPath.Text = dlg.SelectedPath;
                bool recursive = chkSubfolders.Checked;

                var entries = _playlist.LoadFolder(dlg.SelectedPath, recursive);

                if (entries.Count == 0)
                {
                    SetStatus("⚠  No audio files found in the selected folder.", Color.Orange);
                    RefreshPlaylistView();
                    return;
                }

                RefreshPlaylistView();
                SetStatus($"Loaded {entries.Count} tracks from: {Path.GetFileName(dlg.SelectedPath)}");
                UpdateInfoPanel();
            }
        }

        private void BtnAddFiles_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Add audio files to playlist";
                dlg.Filter = "Audio files|*.mp3;*.wav;*.flac;*.aac;*.ogg;*.m4a;*.wma;*.opus|All files|*.*";
                dlg.Multiselect = true;

                if (dlg.ShowDialog() != DialogResult.OK) return;

                _playlist.AddFiles(dlg.FileNames);
                RefreshPlaylistView();
                SetStatus($"Added {dlg.FileNames.Length} file(s). Total: {_playlist.Count} tracks.");
            }
        }

        private void BtnClearPlaylist_Click(object sender, EventArgs e)
        {
            if (_playlist.Count == 0) return;
            var result = MessageBox.Show(
                "Clear the entire playlist?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _streamCts?.Cancel();
            _engine.StopMusic();
            _musicPaused = false;
            _playingStream = false;
            _playlist.Clear();
            RefreshPlaylistView();
            lblNowPlaying.Text = "— No track selected —";
            trkSeek.Value = 0;
            lblMusicPos.Text = "0:00";
            lblMusicDur.Text = "/ 0:00";
            UpdateInfoPanel();
            SetStatus("Playlist cleared.");
            UpdateUI();
        }

        // ══ PLAYLIST – LISTVIEW ═══════════════════════════════════════════════
        private void RefreshPlaylistView()
        {
            lstPlaylist.BeginUpdate();
            lstPlaylist.Items.Clear();

            var queue = _playlist.Queue;
            for (int i = 0; i < queue.Count; i++)
            {
                var entry = queue[i];
                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(entry.Title);
                item.SubItems.Add(entry.Ext);
                item.Tag = i; // queue index

                if (i == _playlist.CurrentIndex)
                {
                    item.BackColor = Color.FromArgb(0, 80, 140);
                    item.ForeColor = Color.White;
                }

                lstPlaylist.Items.Add(item);
            }

            int count = _playlist.Count;
            lblTrackCount.Text = count == 1 ? "1 track" : $"{count} tracks";
            lstPlaylist.EndUpdate();
        }

        private void HighlightCurrentInList()
        {
            foreach (ListViewItem item in lstPlaylist.Items)
            {
                int qi = (int)item.Tag;
                if (qi == _playlist.CurrentIndex)
                {
                    item.BackColor = Color.FromArgb(0, 80, 140);
                    item.ForeColor = Color.White;
                    item.EnsureVisible();
                }
                else
                {
                    item.BackColor = Color.FromArgb(34, 34, 38);
                    item.ForeColor = Color.FromArgb(210, 210, 215);
                }
            }
        }

        private void LstPlaylist_DoubleClick(object sender, EventArgs e)
        {
            if (lstPlaylist.SelectedItems.Count == 0) return;
            int qi = (int)lstPlaylist.SelectedItems[0].Tag;
            var entry = _playlist.GoTo(qi);
            if (entry != null) PlayEntry(entry);
        }

        private void LstPlaylist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && lstPlaylist.SelectedItems.Count > 0)
            {
                int qi = (int)lstPlaylist.SelectedItems[0].Tag;
                bool isCurrent = (qi == _playlist.CurrentIndex);
                _playlist.RemoveAt(qi);
                if (isCurrent) { _engine.StopMusic(); _musicPaused = false; _playingStream = false; }
                RefreshPlaylistView();
                UpdateUI();
            }
            else if (e.KeyCode == Keys.Return && lstPlaylist.SelectedItems.Count > 0)
            {
                LstPlaylist_DoubleClick(sender, e);
            }
        }

        // ══ PLAYLIST – MODE TOGGLES ═══════════════════════════════════════════
        private void BtnShuffle_Click(object sender, EventArgs e)
        {
            bool newState = !_playlist.ShuffleEnabled;
            _playlist.SetShuffle(newState);
            SetToggle(btnShuffle, newState);
            RefreshPlaylistView();
            UpdateInfoPanel();
            SetStatus(newState ? "Shuffle ON" : "Shuffle OFF");
        }

        private void BtnRepeatAll_Click(object sender, EventArgs e)
        {
            bool newState = !_playlist.RepeatAll;
            _playlist.SetRepeatAll(newState);
            SetToggle(btnRepeatAll, newState);
            if (newState) SetToggle(btnRepeatOne, false);
            UpdateInfoPanel();
            SetStatus(newState ? "Repeat All ON" : "Repeat All OFF");
        }

        private void BtnRepeatOne_Click(object sender, EventArgs e)
        {
            bool newState = !_playlist.RepeatOne;
            _playlist.SetRepeatOne(newState);
            SetToggle(btnRepeatOne, newState);
            if (newState) SetToggle(btnRepeatAll, false);
            UpdateInfoPanel();
            SetStatus(newState ? "Repeat One ON" : "Repeat One OFF");
        }

        private void SetToggle(Button btn, bool on)
        {
            btn.Tag = on;
            btn.BackColor = on ? ClrToggleOn : ClrToggleOff;
        }

        // ══ TRANSPORT BUTTONS ═════════════════════════════════════════════════
        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (!_engineRunning) { SetStatus("⚠  Start the engine first.", Color.Orange); return; }

            // Resume if paused
            if (_musicPaused && _engine.IsMusicLoaded)
            {
                _engine.ResumeMusic();
                _musicPaused = false;
                SetStatus($"▶  {CurrentPlaybackTitle}");
                UpdateUI();
                return;
            }

            // Play current playlist track
            var entry = _playlist.CurrentTrack;
            if (entry == null)
            {
                SetStatus("⚠  No tracks in playlist. Load a folder first.", Color.Orange);
                return;
            }
            PlayEntry(entry);
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            if (!_engine.IsMusicLoaded) return;
            if (_musicPaused)
            {
                _engine.ResumeMusic();
                _musicPaused = false;
                SetStatus($"▶  {CurrentPlaybackTitle}");
            }
            else
            {
                _engine.PauseMusic();
                _musicPaused = true;
                SetStatus("⏸  Paused");
            }
            UpdateUI();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _streamCts?.Cancel();
            _engine.StopMusic();
            _musicPaused = false;
            _playingStream = false;
            trkSeek.Value = 0;
            lblMusicPos.Text = "0:00";
            lblMusicDur.Text = "/ 0:00";
            SetStatus("⏹  Stopped");
            UpdateUI();
        }

        private void BtnPrev_Click(object sender, EventArgs e)
        {
            if (!_engineRunning) return;

            // If more than 3 s into a track, restart it instead of going back
            if (_engine.IsMusicLoaded && _engine.MusicPosition.TotalSeconds > 3)
            {
                _engine.SeekMusic(TimeSpan.Zero);
                return;
            }

            var entry = _playlist.Previous();
            if (entry != null) PlayEntry(entry);
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (!_engineRunning) return;
            var entry = _playlist.Next();
            if (entry != null) PlayEntry(entry);
            else
            {
                _engine.StopMusic();
                _musicPaused = false;
                _playingStream = false;
                SetStatus("⏹  End of playlist.");
                UpdateUI();
            }
        }

        // ══ PLAYBACK CORE ═════════════════════════════════════════════════════
        private void PlayEntry(PlaylistEntry entry)
        {
            if (!_engineRunning) return;

            try
            {
                _engine.LoadMusic(entry.FilePath, chkLoop.Checked);
                _engine.MusicVolume = trkMusicVol.Value / 100f;
                _musicPaused = false;
                _playingStream = false;

                trkSeek.Maximum = Math.Max(1, (int)_engine.MusicDuration.TotalSeconds);
                lblMusicDur.Text = $"/ {FormatTime(_engine.MusicDuration)}";
                lblNowPlaying.Text = entry.Title;

                HighlightCurrentInList();
                UpdateInfoPanel();
                SetStatus($"▶  {entry.Title}");
                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot play:\n{entry.FilePath}\n\n{ex.Message}",
                    "Playback Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Try skipping to next track automatically
                var next = _playlist.Next();
                if (next != null) PlayEntry(next);
            }
        }

        // ══ AUTO-ADVANCE ON TRACK END ═════════════════════════════════════════
        //
        // MusicFinished is raised on a ThreadPool thread (AudioEngine design).
        // We must NOT call LoadMusic/PlayEntry synchronously here — the audio
        // render thread may still be finishing its Read() and RemoveMixerInput
        // would deadlock.
        //
        // Safe pattern:
        //   1. BeginInvoke  → hop to UI thread (non-blocking from pool thread)
        //   2. Short Timer  → let the audio thread finish its current buffer (~150 ms)
        //   3. Timer.Tick   → NOW safe to call PlayEntry / LoadMusic
        //
        private void Engine_MusicFinished(object sender, EventArgs e)
        {
            if (_closing || !IsHandleCreated) return;

            // Always arrives on a pool thread – BeginInvoke is non-blocking here
            BeginInvoke(new Action(() =>
            {
                _musicPaused = false;

                // A finished stream must not advance the folder playlist.
                if (_playingStream)
                {
                    _playingStream = false;
                    _engine.StopMusic();
                    lblNowPlaying.Text = "— No track selected —";
                    lblMusicPos.Text = "0:00";
                    lblMusicDur.Text = "/ 0:00";
                    trkSeek.Value = 0;
                    SetStatus("⏹  Stream finished.");
                    UpdateUI();
                    return;
                }

                var next = _playlist.Next();
                if (next == null)
                {
                    trkSeek.Value = 0;
                    lblMusicPos.Text = "0:00";
                    SetStatus("⏹  Playlist finished.");
                    UpdateUI();
                    return;
                }

                // Short delay: give the audio thread ~150 ms to exit Read() cleanly
                var t = new System.Windows.Forms.Timer { Interval = 150 };
                t.Tick += (ts, te) =>
                {
                    t.Stop();
                    t.Dispose();
                    PlayEntry(next);   // safe: audio thread has returned by now
                };
                t.Start();
            }));
        }

        // ══ SEEK ══════════════════════════════════════════════════════════════
        private void TrkSeek_Scroll(object sender, EventArgs e)
        {
            if (!_engine.IsMusicLoaded || !_engine.IsMusicSeekable) return;
            _engine.SeekMusic(TimeSpan.FromSeconds(trkSeek.Value));
        }

        // ══ POSITION TIMER ════════════════════════════════════════════════════
        private void TmrPosition_Tick(object sender, EventArgs e)
        {
            if (!_engine.IsMusicLoaded) return;
            var pos = _engine.MusicPosition;
            lblMusicPos.Text = FormatTime(pos);
            if (_engine.IsMusicSeekable && _engine.MusicDuration.TotalSeconds > 0)
                trkSeek.Value = Math.Min(trkSeek.Maximum, (int)pos.TotalSeconds);
        }

        // ══ VOLUME / MUTE ═════════════════════════════════════════════════════
        private void TrkMusicVol_Changed(object sender, EventArgs e)
        {
            _engine.MusicVolume = trkMusicVol.Value / 100f;
            lblMusicVol.Text = $"{trkMusicVol.Value}%";
        }

        private void TrkMicVol_Changed(object sender, EventArgs e)
        {
            _engine.MicVolume = trkMicVol.Value / 100f;
            lblMicVol.Text = $"{trkMicVol.Value}%";
        }

        private void ChkMuteMusic_Changed(object sender, EventArgs e) =>
            _engine.MusicMuted = chkMuteMusic.Checked;

        private void ChkMuteMic_Changed(object sender, EventArgs e) =>
            _engine.MicMuted = chkMuteMic.Checked;

        // ══ MICROPHONE ════════════════════════════════════════════════════════
        private void BtnEnableMic_Click(object sender, EventArgs e)
        {
            if (!_engineRunning) { SetStatus("⚠  Start the engine first.", Color.Orange); return; }
            if (cmbMicInput.SelectedIndex < 0) { SetStatus("⚠  No microphone selected.", Color.Orange); return; }
            try
            {
                _engine.EnableMic(cmbMicInput.SelectedIndex);
                _engine.MicVolume = trkMicVol.Value / 100f;
                SetStatus($"🎤  Mic active: {_inputs[cmbMicInput.SelectedIndex].Name}");
                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot enable microphone:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDisableMic_Click(object sender, EventArgs e)
        {
            _engine.DisableMic();
            SetStatus("Mic disabled.");
            UpdateUI();
        }

        // ══ INFO PANEL UPDATE ═════════════════════════════════════════════════
        private void UpdateInfoPanel()
        {
            var track = _playlist.CurrentTrack;

            _lblInfoFolderVal.Text =
                string.IsNullOrEmpty(txtFolderPath.Text) ? "—" :
                Path.GetFileName(txtFolderPath.Text.TrimEnd('\\', '/'));

            _lblInfoTrackVal.Text = track?.Title ?? "—";

            _lblInfoIdxVal.Text =
                _playlist.Count > 0
                    ? $"{_playlist.CurrentIndex + 1} / {_playlist.Count}"
                    : "—";

            string mode = "Normal";
            if (_playlist.ShuffleEnabled) mode = "Shuffle";
            if (_playlist.RepeatAll) mode = "Repeat All";
            if (_playlist.RepeatOne) mode = "Repeat One";
            _lblInfoModeVal.Text = mode;
        }

        // ══ UI STATE ══════════════════════════════════════════════════════════
        private void UpdateUI()
        {
            bool eng = _engineRunning;
            bool music = _engine.IsMusicLoaded;
            bool hasTracks = _playlist.HasTracks;
            bool busy = _streamBusy;

            btnPlay.Enabled = eng && !busy;
            btnPause.Enabled = eng && music && !busy;
            btnStop.Enabled = eng && (music || busy);
            btnPrev.Enabled = eng && hasTracks && !busy;
            btnNext.Enabled = eng && hasTracks && !busy;
            trkSeek.Enabled = eng && music && _engine.IsMusicSeekable;
            trkMusicVol.Enabled = eng;
            chkMuteMusic.Enabled = eng;

            btnEnableMic.Enabled = eng;
            btnDisableMic.Enabled = eng;
            trkMicVol.Enabled = eng;
            chkMuteMic.Enabled = eng;

            cmbVbCable.Enabled = !eng;
            cmbMonitor.Enabled = !eng;

            btnPause.Text = _musicPaused ? "⏵" : "⏸";
        }

        // ══ HELPERS ═══════════════════════════════════════════════════════════
        private void SetStatus(string msg, Color? color = null)
        {
            tsslStatus.Text = msg;
            tsslStatus.ForeColor = color ?? Color.FromArgb(180, 180, 180);
        }

        private string CurrentPlaybackTitle =>
            _playingStream ? _streamTitle : (_playlist.CurrentTrack?.Title ?? "Playing");

        private static string FirstLine(string text) =>
            string.IsNullOrEmpty(text) ? "Unknown error" : text.Split('\n')[0].Trim();

        private static string FormatTime(TimeSpan t) =>
            t.TotalHours >= 1
                ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes}:{t.Seconds:D2}";
    }
}