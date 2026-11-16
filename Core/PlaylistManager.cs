using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VirtualMicMixer.Core
{
    public class PlaylistEntry
    {
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Ext { get; set; }
        public int OriginalIndex { get; set; }
    }

    public class PlaylistManager
    {
        private static readonly HashSet<string> AudioExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".opus"
        };

        private readonly List<PlaylistEntry> _master = new List<PlaylistEntry>();
        private readonly Random _rng = new Random();
        private List<PlaylistEntry> _queue = new List<PlaylistEntry>();
        private int _currentIndex = -1;

        public bool ShuffleEnabled { get; private set; }
        public bool RepeatAll { get; private set; }
        public bool RepeatOne { get; private set; }

        public int Count => _master.Count;
        public bool HasTracks => _master.Count > 0;
        public int CurrentIndex => _currentIndex;

        public PlaylistEntry CurrentTrack =>
            (_currentIndex >= 0 && _currentIndex < _queue.Count) ? _queue[_currentIndex] : null;

        public IReadOnlyList<PlaylistEntry> Queue => _queue.AsReadOnly();

        public List<PlaylistEntry> LoadFolder(string folder, bool recursive = false)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                Clear();
                return new List<PlaylistEntry>();
            }

            var files = Directory.GetFiles(folder, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(f => AudioExts.Contains(Path.GetExtension(f)))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _master.Clear();
            for (int i = 0; i < files.Count; i++)
            {
                _master.Add(new PlaylistEntry
                {
                    FilePath = files[i],
                    Title = Path.GetFileNameWithoutExtension(files[i]),
                    Ext = Path.GetExtension(files[i]).ToUpperInvariant().TrimStart('.'),
                    OriginalIndex = i
                });
            }

            RebuildQueue();
            _currentIndex = _queue.Count > 0 ? 0 : -1;
            return _queue.ToList();
        }

        public void AddFiles(IEnumerable<string> files)
        {
            if (files == null) return;

            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file) || !File.Exists(file)) continue;
                if (!AudioExts.Contains(Path.GetExtension(file))) continue;

                _master.Add(new PlaylistEntry
                {
                    FilePath = file,
                    Title = Path.GetFileNameWithoutExtension(file),
                    Ext = Path.GetExtension(file).ToUpperInvariant().TrimStart('.'),
                    OriginalIndex = _master.Count
                });
            }

            RebuildQueue();
            if (_queue.Count == 0)
                _currentIndex = -1;
            else if (_currentIndex < 0)
                _currentIndex = 0;
        }

        public void Clear()
        {
            _master.Clear();
            _queue.Clear();
            _currentIndex = -1;
        }

        public PlaylistEntry GoTo(int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= _queue.Count) return null;
            _currentIndex = queueIndex;
            return _queue[_currentIndex];
        }

        public PlaylistEntry Next()
        {
            if (_queue.Count == 0) return null;
            if (_currentIndex < 0) _currentIndex = 0;

            if (RepeatOne)
                return _queue[_currentIndex];

            int next = _currentIndex + 1;
            if (next >= _queue.Count)
            {
                if (RepeatAll)
                {
                    if (ShuffleEnabled) RebuildQueue();
                    next = 0;
                }
                else
                {
                    _currentIndex = _queue.Count - 1;
                    return null;
                }
            }

            _currentIndex = next;
            return _queue[_currentIndex];
        }

        public PlaylistEntry Previous()
        {
            if (_queue.Count == 0) return null;
            if (_currentIndex < 0) _currentIndex = 0;

            int prev = _currentIndex - 1;
            if (prev < 0)
            {
                if (RepeatAll)
                    prev = _queue.Count - 1;
                else
                    prev = 0;
            }

            _currentIndex = prev;
            return _queue[_currentIndex];
        }

        public void SetShuffle(bool enabled)
        {
            ShuffleEnabled = enabled;
            var current = CurrentTrack;
            RebuildQueue();

            if (current != null)
            {
                int currentIndex = _queue.FindIndex(e => e.FilePath == current.FilePath);
                _currentIndex = currentIndex >= 0 ? currentIndex : 0;
            }
            else if (_queue.Count > 0)
            {
                _currentIndex = 0;
            }
            else
            {
                _currentIndex = -1;
            }
        }

        public void SetRepeatAll(bool enabled)
        {
            RepeatAll = enabled;
            if (enabled) RepeatOne = false;
        }

        public void SetRepeatOne(bool enabled)
        {
            RepeatOne = enabled;
            if (enabled) RepeatAll = false;
        }

        public void RemoveAt(int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= _queue.Count) return;

            var entry = _queue[queueIndex];
            _master.Remove(entry);
            _queue.RemoveAt(queueIndex);

            if (_queue.Count == 0)
            {
                _currentIndex = -1;
                return;
            }

            if (_currentIndex >= _queue.Count)
                _currentIndex = _queue.Count - 1;
            else if (_currentIndex > queueIndex)
                _currentIndex--;
        }

        private void RebuildQueue()
        {
            if (ShuffleEnabled)
            {
                _queue = _master.OrderBy(_ => _rng.Next()).ToList();
            }
            else
            {
                _queue = _master.OrderBy(e => e.OriginalIndex).ToList();
            }
        }
    }
}