using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VirtualMicMixer.Core
{
    /// <summary>
    /// Streams a remote audio URL through ffmpeg into a ring buffer and exposes it
    /// as an <see cref="ISampleProvider"/> in the mixer's 48 kHz / 16-bit / stereo
    /// format.  Used as a fallback when Media Foundation cannot open a stream
    /// (live streams, HLS, WebM/Opus …) and supports already-running live sources,
    /// which is why there is no position/duration/seek support here.
    /// </summary>
    public sealed class ProcessAudioStream : IDisposable
    {
        private static readonly WaveFormat PcmFormat = new WaveFormat(48000, 16, 2);

        private readonly Process _ffmpeg;
        private readonly BufferedWaveProvider _buffer;
        private readonly Thread _pumpThread;
        private readonly object _stderrLock = new object();
        private readonly StringBuilder _stderr = new StringBuilder();

        private volatile bool _ended;
        private volatile bool _disposed;

        /// <summary>Mixer-ready audio source (float stereo 48 kHz).</summary>
        public ISampleProvider Source { get; }

        /// <summary>True once ffmpeg has exited and all remaining buffered audio was consumed.</summary>
        public bool Ended => _ended;

        /// <summary>Bytes currently buffered ahead of playback (0 while ffmpeg is still connecting).</summary>
        public long BufferedBytes => _buffer.BufferedBytes;

        public string ErrorText
        {
            get { lock (_stderrLock) return _stderr.ToString().Trim(); }
        }

        public ProcessAudioStream(string ffmpegPath, string sourceUrl)
        {
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments =
                    "-hide_banner -loglevel error -nostdin -i " + Quote(sourceUrl) +
                    " -vn -ac 2 -ar 48000 -f s16le pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8
            };

            _buffer = new BufferedWaveProvider(PcmFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(10),
                DiscardOnBufferOverflow = true
            };

            Source = new BufferedPcmSource(this, _buffer.ToSampleProvider());

            _ffmpeg = new Process { StartInfo = psi };
            _ffmpeg.ErrorDataReceived += OnErrorData;
            if (!_ffmpeg.Start())
                throw new InvalidOperationException("Could not start ffmpeg.");
            _ffmpeg.BeginErrorReadLine();

            _pumpThread = new Thread(PumpOutput)
            {
                IsBackground = true,
                Name = "ffmpeg-pcm-pump"
            };
            _pumpThread.Start();
        }

        private void OnErrorData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            lock (_stderrLock)
            {
                _stderr.AppendLine(e.Data);
                // Keep only the tail so a long-running stream cannot grow unbounded.
                if (_stderr.Length > 4000) _stderr.Remove(0, _stderr.Length - 4000);
            }
        }

        private void PumpOutput()
        {
            try
            {
                var stdout = _ffmpeg.StandardOutput.BaseStream;
                var chunk = new byte[8192];
                int read;
                while (!_disposed && (read = stdout.Read(chunk, 0, chunk.Length)) > 0)
                    _buffer.AddSamples(chunk, 0, read);
            }
            catch (Exception)
            {
                // Process was killed (stop/dispose) or the pipe broke – treat as end of stream.
            }
            finally
            {
                _ended = true;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { if (!_ffmpeg.HasExited) _ffmpeg.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }

            try { _ffmpeg.Dispose(); }
            catch (InvalidOperationException) { }
        }

        private static string Quote(string value) =>
            "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";

        /// <summary>
        /// Returns audio (zero-padded during temporary underruns) while the stream
        /// is alive, and 0 only after ffmpeg ended and the buffer is drained – that
        /// zero is what lets AudioEngine raise MusicFinished.
        /// </summary>
        private sealed class BufferedPcmSource : ISampleProvider
        {
            private readonly ProcessAudioStream _owner;
            private readonly ISampleProvider _reader;

            public WaveFormat WaveFormat => _reader.WaveFormat;

            public BufferedPcmSource(ProcessAudioStream owner, ISampleProvider reader)
            {
                _owner = owner;
                _reader = reader;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                if (_owner._ended && _owner._buffer.BufferedBytes == 0)
                    return 0;

                // BufferedWaveProvider zero-fills short reads. A partially filled
                // buffer (normal while streaming) must never be reported as a short
                // read: MixingSampleProvider removes inputs that return < count.
                return _reader.Read(buffer, offset, count);
            }
        }
    }
}
