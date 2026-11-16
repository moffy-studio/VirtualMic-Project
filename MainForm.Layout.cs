using System;
using System.Drawing;
using System.Windows.Forms;

namespace VirtualMicMixer
{
    /// <summary>
    /// Code-built UI.  Kept out of MainForm.Designer.cs on purpose: the Visual
    /// Studio WinForms designer cannot parse helper-based layout code and will
    /// wipe InitializeComponent() — moving it here keeps the UI safe.
    /// </summary>
    partial class MainForm
    {
        // ── Device selectors ─────────────────────────────────────────────────
        private Label lblVbCable, lblMonitor, lblMicInput;
        private ComboBox cmbVbCable, cmbMonitor, cmbMicInput;
        private Button btnInitEngine;
        private Label lblEngineStatus;

        // ── Music / transport ────────────────────────────────────────────────
        private GroupBox grpMusic;
        private Label lblNowPlaying;
        private Button btnPlay, btnPause, btnStop, btnPrev, btnNext;
        private CheckBox chkLoop, chkMuteMusic;
        private TrackBar trkMusicVol;
        private Label lblMusicVol, lblMusicPos, lblMusicDur;
        private TrackBar trkSeek;
        private Timer tmrPosition;

        // ── Tabs: local playlist / stream source ──────────────────────────────
        private TabControl tabMain;
        private TabPage tabLocal, tabStream;

        // ── Stream panel ──────────────────────────────────────────────────────
        private Label lblStreamUrl, lblYtDlpStatus, lblCookies, lblStreamHint, lblCookieState;
        private TextBox txtStreamUrl;
        private Button btnStream, btnBrowseYtDlp, btnCookiesFile, btnCookiesClear;
        private ComboBox cmbCookies;
        private LinkLabel lnkCookieHelp;

        // ── Playlist panel ────────────────────────────────────────────────────
        private Label lblFolder;
        private TextBox txtFolderPath;
        private Button btnBrowseFolder, btnAddFiles, btnClearPlaylist;
        private CheckBox chkSubfolders;
        private Button btnShuffle, btnRepeatAll, btnRepeatOne;
        private ListView lstPlaylist;
        private Label lblTrackCount;

        // ── Mic section ──────────────────────────────────────────────────────
        private GroupBox grpMic;
        private Button btnEnableMic, btnDisableMic;
        private CheckBox chkMuteMic;
        private TrackBar trkMicVol;
        private Label lblMicVol;

        // ── Status bar ───────────────────────────────────────────────────────
        private StatusStrip statusStrip;
        private ToolStripStatusLabel tsslStatus;

        // ── Dynamic info labels ──────────────────────────────────────────────
        private Label _lblInfoFolderVal, _lblInfoTrackVal, _lblInfoIdxVal, _lblInfoModeVal;

        private void BuildLayout()
        {
            if (components == null) components = new System.ComponentModel.Container();

            this.SuspendLayout();

            // ── Form ─────────────────────────────────────────────────────────
            this.Text = "Virtual Mic Mixer  –  VB-CABLE + NAudio";
            this.Size = new Size(860, 640);
            this.MinimumSize = new Size(860, 640);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 9f);
            this.BackColor = Color.FromArgb(28, 28, 32);
            this.ForeColor = Color.FromArgb(220, 220, 220);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ══ DEVICE PANEL ════════════════════════════════════════════════
            var pnlDevices = MakePanel(8, 8, 838, 110);
            pnlDevices.BorderStyle = BorderStyle.FixedSingle;

            var lblDev = MakeLabel("Audio Devices", 8, 6, bold: true);
            lblDev.ForeColor = Color.FromArgb(100, 180, 255);

            lblVbCable = MakeLabel("VB-CABLE Output (virtual mic):", 8, 28);
            cmbVbCable = MakeCombo(232, 25, 300);

            lblMonitor = MakeLabel("Monitor / Headphones:", 8, 55);
            cmbMonitor = MakeCombo(232, 52, 300);

            lblMicInput = MakeLabel("Microphone Input:", 8, 82);
            cmbMicInput = MakeCombo(232, 79, 240);

            btnInitEngine = MakeButton("▶  Start Engine", 700, 50, 130, 32);
            btnInitEngine.BackColor = Color.FromArgb(0, 120, 60);
            btnInitEngine.Click += BtnInitEngine_Click;

            lblEngineStatus = MakeLabel("Engine stopped", 700, 88, bold: false);
            lblEngineStatus.ForeColor = Color.FromArgb(160, 160, 160);
            lblEngineStatus.Width = 130;

            pnlDevices.Controls.AddRange(new Control[]
            {
                lblDev, lblVbCable, cmbVbCable,
                lblMonitor, cmbMonitor,
                lblMicInput, cmbMicInput,
                btnInitEngine, lblEngineStatus
            });

            // ══ TABS: LOCAL PLAYLIST / STREAM ═══════════════════════════════
            tabMain = new TabControl
            {
                Location = new Point(8, 126),
                Size = new Size(420, 340),
                ItemSize = new Size(170, 26),
                SizeMode = TabSizeMode.Fixed
            };

            tabLocal = MakeTabPage("  📂  Local Playlist");
            tabStream = MakeTabPage("  🌐  Stream");
            tabMain.TabPages.Add(tabLocal);
            tabMain.TabPages.Add(tabStream);

            // ── Local tab: folder playlist ────────────────────────────────────
            lblFolder = MakeLabel("Folder:", 10, 10);
            txtFolderPath = new TextBox
            {
                Location = new Point(60, 7),
                Size = new Size(248, 22),
                BackColor = Color.FromArgb(48, 48, 52),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };
            btnBrowseFolder = MakeButton("Browse…", 314, 5, 82, 26);
            btnBrowseFolder.Click += BtnBrowseFolder_Click;

            chkSubfolders = new CheckBox
            {
                Text = "Include subfolders",
                Location = new Point(8, 38),
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 180, 180)
            };

            btnAddFiles = MakeButton("+ Add Files", 196, 34, 104, 24);
            btnAddFiles.Click += BtnAddFiles_Click;

            btnClearPlaylist = MakeButton("✕ Clear", 306, 34, 90, 24);
            btnClearPlaylist.BackColor = Color.FromArgb(100, 35, 35);
            btnClearPlaylist.Click += BtnClearPlaylist_Click;

            // Mode toggle buttons
            btnShuffle = MakeToggleButton("⇀ Shuffle", 8, 66, 88, 26);
            btnShuffle.Click += BtnShuffle_Click;

            btnRepeatAll = MakeToggleButton("↺ Repeat All", 104, 66, 100, 26);
            btnRepeatAll.Click += BtnRepeatAll_Click;

            btnRepeatOne = MakeToggleButton("↻ Repeat One", 212, 66, 104, 26);
            btnRepeatOne.Click += BtnRepeatOne_Click;

            lblTrackCount = MakeLabel("0 tracks", 322, 71);
            lblTrackCount.ForeColor = Color.FromArgb(130, 130, 130);
            lblTrackCount.AutoSize = false;
            lblTrackCount.Size = new Size(70, 16);
            lblTrackCount.AutoEllipsis = true;

            // Playlist ListView
            lstPlaylist = new ListView
            {
                Location = new Point(6, 98),
                Size = new Size(396, 202),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = false,
                HideSelection = false,
                BackColor = Color.FromArgb(34, 34, 38),
                ForeColor = Color.FromArgb(210, 210, 215),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8.5f)
            };
            lstPlaylist.Columns.Add("#", 36);
            lstPlaylist.Columns.Add("Title", 296);
            lstPlaylist.Columns.Add("Type", 50);
            lstPlaylist.DoubleClick += LstPlaylist_DoubleClick;
            lstPlaylist.KeyDown += LstPlaylist_KeyDown;

            tabLocal.Controls.AddRange(new Control[]
            {
                lblFolder, txtFolderPath, btnBrowseFolder,
                chkSubfolders, btnAddFiles, btnClearPlaylist,
                btnShuffle, btnRepeatAll, btnRepeatOne, lblTrackCount,
                lstPlaylist
            });

            // ── Stream tab: link + yt-dlp options ─────────────────────────────
            lblStreamUrl = MakeLabel("Link:", 8, 12);

            txtStreamUrl = new TextBox
            {
                Location = new Point(46, 9),
                Size = new Size(356, 22),
                BackColor = Color.FromArgb(48, 48, 52),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtStreamUrl.KeyDown += TxtStreamUrl_KeyDown;

            btnStream = MakeButton("▶  Stream", 8, 42, 96, 30);
            btnStream.BackColor = Color.FromArgb(0, 110, 180);
            btnStream.Click += BtnStream_Click;

            lblYtDlpStatus = MakeLabel("yt-dlp: —", 112, 50);
            lblYtDlpStatus.AutoSize = false;
            lblYtDlpStatus.Size = new Size(88, 16);
            lblYtDlpStatus.AutoEllipsis = true;
            lblYtDlpStatus.ForeColor = Color.FromArgb(150, 150, 150);

            btnBrowseYtDlp = MakeButton("…", 204, 42, 30, 28);
            btnBrowseYtDlp.Click += BtnBrowseYtDlp_Click;

            lblCookies = MakeLabel("Cookies:", 234, 48);

            cmbCookies = MakeCombo(288, 44, 60);
            cmbCookies.Items.AddRange(new object[] { "none", "chrome", "edge", "firefox" });
            cmbCookies.SelectedIndexChanged += CmbCookies_SelectedIndexChanged;

            btnCookiesFile = MakeButton("📄", 350, 42, 30, 28);
            btnCookiesFile.Click += BtnCookiesFile_Click;
            _toolTip.SetToolTip(btnCookiesFile, "Load a cookies.txt file (Netscape format) exported from your browser");

            btnCookiesClear = MakeButton("✕", 382, 42, 26, 28);
            btnCookiesClear.Click += BtnCookiesClear_Click;
            _toolTip.SetToolTip(btnCookiesClear, "Clear the loaded cookies.txt file");

            lblStreamHint = new Label
            {
                Location = new Point(8, 86),
                Size = new Size(396, 96),
                Text = "Paste a YouTube / SoundCloud link, or a direct audio URL.\r\n" +
                       "YouTube links need yt-dlp; live links also need ffmpeg.\r\n\r\n" +
                       "If YouTube asks for a sign-in check, provide your YouTube\r\n" +
                       "cookies: pick a browser above (close it first) or load a\r\n" +
                       "cookies.txt file with the 📄 button.",
                ForeColor = Color.FromArgb(130, 130, 130),
                AutoSize = false
            };

            lblCookieState = new Label
            {
                Location = new Point(8, 190),
                Size = new Size(396, 16),
                Text = "Cookies: none",
                ForeColor = Color.FromArgb(150, 150, 150),
                AutoEllipsis = true,
                AutoSize = false
            };

            lnkCookieHelp = new LinkLabel
            {
                Location = new Point(8, 210),
                AutoSize = true,
                Text = "How do I get YouTube cookies?",
                LinkColor = Color.FromArgb(120, 180, 255),
                ActiveLinkColor = Color.White,
                VisitedLinkColor = Color.FromArgb(120, 180, 255)
            };
            lnkCookieHelp.LinkClicked += LnkCookieHelp_LinkClicked;

            tabStream.Controls.AddRange(new Control[]
            {
                lblStreamUrl, txtStreamUrl, btnStream,
                lblYtDlpStatus, btnBrowseYtDlp, lblCookies, cmbCookies,
                btnCookiesFile, btnCookiesClear, lblStreamHint,
                lblCookieState, lnkCookieHelp
            });

            // ══ MUSIC TRANSPORT GROUP (right) ════════════════════════════════
            grpMusic = MakeGroup("  🎵  Now Playing", 436, 126, 410, 340);

            // Now-playing label
            lblNowPlaying = new Label
            {
                Location = new Point(10, 20),
                Size = new Size(386, 32),
                Text = "— No track selected —",
                ForeColor = Color.FromArgb(160, 200, 255),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                AutoEllipsis = true
            };

            // Transport row
            btnPrev = MakeButton("⏮", 10, 58, 48, 36);
            btnPrev.Font = new Font("Segoe UI", 12f);
            btnPrev.Click += BtnPrev_Click;

            btnPlay = MakeButton("⏵", 66, 58, 60, 36);
            btnPlay.BackColor = Color.FromArgb(0, 130, 70);
            btnPlay.Font = new Font("Segoe UI", 13f);
            btnPlay.Click += BtnPlay_Click;

            btnPause = MakeButton("⏸", 134, 58, 60, 36);
            btnPause.Font = new Font("Segoe UI", 12f);
            btnPause.Click += BtnPause_Click;

            btnStop = MakeButton("⏹", 202, 58, 48, 36);
            btnStop.BackColor = Color.FromArgb(140, 40, 40);
            btnStop.Font = new Font("Segoe UI", 12f);
            btnStop.Click += BtnStop_Click;

            btnNext = MakeButton("⏭", 258, 58, 48, 36);
            btnNext.Font = new Font("Segoe UI", 12f);
            btnNext.Click += BtnNext_Click;

            chkLoop = new CheckBox
            {
                Text = "Loop track",
                Location = new Point(316, 66),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            // Seek row
            var lblSeekHdr = MakeLabel("Position:", 10, 108);
            trkSeek = new TrackBar
            {
                Location = new Point(78, 102),
                Size = new Size(238, 30),
                Minimum = 0,
                Maximum = 1000,
                TickStyle = TickStyle.None,
                BackColor = Color.FromArgb(40, 40, 44)
            };
            trkSeek.Scroll += TrkSeek_Scroll;

            lblMusicPos = MakeLabel("0:00", 322, 108);
            lblMusicPos.Width = 38;
            lblMusicDur = MakeLabel("/ 0:00", 362, 108);
            lblMusicDur.Width = 50;

            // Volume row
            var lblMusVolHdr = MakeLabel("Volume:", 10, 146);
            trkMusicVol = MakeVolumeTrack(78, 140, 218);
            trkMusicVol.ValueChanged += TrkMusicVol_Changed;
            lblMusicVol = MakeLabel("100%", 302, 146);
            lblMusicVol.Width = 45;

            chkMuteMusic = new CheckBox
            {
                Text = "Mute",
                Location = new Point(352, 147),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200)
            };
            chkMuteMusic.CheckedChanged += ChkMuteMusic_Changed;

            // ── Info panel ────────────────────────────────────────────────────
            var pnlInfo = new Panel
            {
                Location = new Point(8, 178),
                Size = new Size(390, 140),
                BackColor = Color.FromArgb(30, 30, 34),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblInfoHdr = MakeLabel("Playlist Info", 8, 6, bold: true);
            lblInfoHdr.ForeColor = Color.FromArgb(100, 180, 255);

            var lblInfoFolderKey = MakeLabel("Folder:", 8, 28);
            _lblInfoFolderVal = new Label
            {
                Location = new Point(65, 28),
                Size = new Size(316, 16),
                Text = "—",
                ForeColor = Color.FromArgb(190, 190, 190),
                AutoEllipsis = true,
                AutoSize = false
            };

            var lblInfoTrackKey = MakeLabel("Track:", 8, 50);
            _lblInfoTrackVal = new Label
            {
                Location = new Point(65, 50),
                Size = new Size(316, 16),
                Text = "—",
                ForeColor = Color.FromArgb(190, 190, 190),
                AutoEllipsis = true,
                AutoSize = false
            };

            var lblInfoIdxKey = MakeLabel("Index:", 8, 72);
            _lblInfoIdxVal = MakeLabel("—", 65, 72);
            _lblInfoIdxVal.ForeColor = Color.FromArgb(190, 190, 190);

            var lblInfoModeKey = MakeLabel("Mode:", 8, 94);
            _lblInfoModeVal = MakeLabel("Normal", 65, 94);
            _lblInfoModeVal.ForeColor = Color.FromArgb(100, 220, 100);

            pnlInfo.Controls.AddRange(new Control[]
            {
                lblInfoHdr,
                lblInfoFolderKey, _lblInfoFolderVal,
                lblInfoTrackKey,  _lblInfoTrackVal,
                lblInfoIdxKey,    _lblInfoIdxVal,
                lblInfoModeKey,   _lblInfoModeVal
            });

            grpMusic.Controls.AddRange(new Control[]
            {
                lblNowPlaying,
                btnPrev, btnPlay, btnPause, btnStop, btnNext, chkLoop,
                lblSeekHdr, trkSeek, lblMusicPos, lblMusicDur,
                lblMusVolHdr, trkMusicVol, lblMusicVol, chkMuteMusic,
                pnlInfo
            });

            // ── Position timer ────────────────────────────────────────────────
            tmrPosition = new Timer(components) { Interval = 250 };
            tmrPosition.Tick += TmrPosition_Tick;

            // ══ MIC GROUP ═══════════════════════════════════════════════════
            grpMic = MakeGroup("  🎤  Microphone", 8, 474, 838, 80);

            btnEnableMic = MakeButton("Enable Mic", 10, 18, 100, 30);
            btnEnableMic.BackColor = Color.FromArgb(0, 100, 160);
            btnEnableMic.Click += BtnEnableMic_Click;

            btnDisableMic = MakeButton("Disable Mic", 118, 18, 100, 30);
            btnDisableMic.BackColor = Color.FromArgb(100, 40, 40);
            btnDisableMic.Click += BtnDisableMic_Click;

            var lblMicVolHdr = MakeLabel("Mic Volume:", 240, 25);
            trkMicVol = MakeVolumeTrack(330, 14, 200);
            trkMicVol.ValueChanged += TrkMicVol_Changed;
            lblMicVol = MakeLabel("100%", 536, 25);
            lblMicVol.Width = 45;

            chkMuteMic = new CheckBox
            {
                Text = "Mute Mic",
                Location = new Point(596, 25),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200)
            };
            chkMuteMic.CheckedChanged += ChkMuteMic_Changed;

            grpMic.Controls.AddRange(new Control[]
            {
                btnEnableMic, btnDisableMic,
                lblMicVolHdr, trkMicVol, lblMicVol, chkMuteMic
            });

            // ══ STATUS BAR ══════════════════════════════════════════════════
            statusStrip = new StatusStrip { BackColor = Color.FromArgb(18, 18, 22) };
            tsslStatus = new ToolStripStatusLabel("Ready  –  Select devices and click Start Engine")
            {
                ForeColor = Color.FromArgb(180, 180, 180),
                Spring = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            statusStrip.Items.Add(tsslStatus);

            // ══ ADD TO FORM ══════════════════════════════════════════════════
            this.Controls.AddRange(new Control[]
            {
                pnlDevices, tabMain, grpMusic, grpMic, statusStrip
            });

            this.ResumeLayout(false);
        }

        // ── UI factory helpers ────────────────────────────────────────────────
        private Panel MakePanel(int x, int y, int w, int h) =>
            new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(40, 40, 44)
            };

        private TabPage MakeTabPage(string text) =>
            new TabPage(text)
            {
                BackColor = Color.FromArgb(36, 36, 40),
                ForeColor = Color.FromArgb(220, 220, 220),
                UseVisualStyleBackColor = false
            };

        private GroupBox MakeGroup(string text, int x, int y, int w, int h) =>
            new GroupBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                ForeColor = Color.FromArgb(180, 210, 255),
                BackColor = Color.FromArgb(36, 36, 40)
            };

        private Label MakeLabel(string text, int x, int y, bool bold = false) =>
            new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = bold ? new Font("Segoe UI", 9f, FontStyle.Bold) : this.Font
            };

        private ComboBox MakeCombo(int x, int y, int w) =>
            new ComboBox
            {
                Location = new Point(x, y),
                Width = w,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(52, 52, 56),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

        private Button MakeButton(string text, int x, int y, int w, int h) =>
            new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(58, 58, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(85, 85, 92) },
                Cursor = Cursors.Hand
            };

        private Button MakeToggleButton(string text, int x, int y, int w, int h)
        {
            var btn = MakeButton(text, x, y, w, h);
            btn.Tag = false;
            return btn;
        }

        private TrackBar MakeVolumeTrack(int x, int y, int w) =>
            new TrackBar
            {
                Location = new Point(x, y),
                Width = w,
                Minimum = 0,
                Maximum = 200,
                Value = 100,
                TickFrequency = 20,
                SmallChange = 5,
                BackColor = Color.FromArgb(42, 42, 46)
            };
    }
}
