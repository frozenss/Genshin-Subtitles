using System;
using System.Collections.Generic;
using System.Globalization;

namespace GI_Subtitles.Core.Overlay
{
    public sealed class LiveOverlaySession
    {
        public const int DefaultOcrIntervalMs = 400;
        public const int UiMinOcrIntervalMs = 200;
        public const int UiMaxOcrIntervalMs = 1000;
        public const int EngineFloorOcrIntervalMs = 1;
        public const string OcrIntervalConfigKey = "OCRInterval";
        public const int HintDurationMs = 2000;
        public const int EnginePairCap = 8;

        private const string HintResourceRecognitionRunning = "Hint_RecognitionRunning";
        private const string HintResourceRecognitionStopped = "Hint_RecognitionStopped";
        private const string HintResourceCaptureRegionBoxed = "Hint_CaptureRegionBoxed";
        private const string HintResourceSubtitlesHidden = "Hint_SubtitlesHidden";
        private const string HintResourceSubtitlesShown = "Hint_SubtitlesShown";
        private const string HintResourceRefreshed = "Hint_Refreshed";
        private const string HintResourceRefreshFoundNoText = "Hint_RefreshFoundNoText";
        private const string HintResourceVoiceSpeed = "Hint_VoiceSpeed";
        private const string HintResourceCaptureRegionMissing = "Hint_CaptureRegionMissing";

        private readonly IOcrIntervalStore _store;
        private readonly IRegionPairStore _pairStore;
        private readonly Func<DateTime> _utcNow;
        private readonly List<RegionPair> _pairs = new List<RegionPair>();
        private readonly List<string> _headers = new List<string>();
        private readonly List<string> _contents = new List<string>();
        private readonly List<int> _recognitionOrders = new List<int>();
        private readonly List<int> _ocrQueue = new List<int>();
        private int _storedMs;
        private DateTime? _hintExpiresAt;
        private DateTime _lastOcrTime = DateTime.MinValue;
        private int? _busyPairIndex;
        private int _recognitionSequence;

        public LiveOverlaySession(IOcrIntervalStore store)
            : this(store, null, null)
        {
        }

        public LiveOverlaySession(IOcrIntervalStore store, Func<DateTime> utcNow)
            : this(store, null, utcNow)
        {
        }

        public LiveOverlaySession(IOcrIntervalStore store, IRegionPairStore pairStore)
            : this(store, pairStore, null)
        {
        }

        public LiveOverlaySession(IOcrIntervalStore store, IRegionPairStore pairStore, Func<DateTime> utcNow)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _pairStore = pairStore;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _storedMs = _store.Read(DefaultOcrIntervalMs);
            SubtitlesVisible = true;
            LoadPairs();
        }

        public IReadOnlyList<RegionPair> Pairs
        {
            get { return _pairs; }
        }

        public IReadOnlyList<int> OcrQueue
        {
            get { return _ocrQueue; }
        }

        public int? BusyOcrPairIndex
        {
            get { return _busyPairIndex; }
        }

        public bool HasValidCapture
        {
            get
            {
                int engineCount = Math.Min(EnginePairCap, _pairs.Count);
                for (int i = 0; i < engineCount; i++)
                {
                    if (_pairs[i].Capture.IsValid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public IReadOnlyList<PairSubtitleBody> PairBodies
        {
            get
            {
                var bodies = new PairSubtitleBody[_pairs.Count];
                for (int i = 0; i < _pairs.Count; i++)
                {
                    OverlayRect display = _pairs[i].Display;
                    string content = _contents[i];
                    bool visible = SubtitlesVisible && display.IsValid && !string.IsNullOrEmpty(content);
                    bodies[i] = new PairSubtitleBody(
                        i,
                        display,
                        _headers[i],
                        content,
                        visible,
                        _recognitionOrders[i]);
                }

                return bodies;
            }
        }

        public event EventHandler HintChanged;

        public bool HintVisible { get; private set; }

        public string HintResourceKey { get; private set; }

        public object[] HintFormatArguments { get; private set; }

        public bool RecognitionRunning { get; private set; }

        public bool SubtitlesVisible { get; private set; }

        public void StartRecognition(bool hasCaptureRegion)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                ShowHint(HintResourceCaptureRegionMissing);
                return;
            }

            RecognitionRunning = true;
            ShowHint(HintResourceRecognitionRunning);
        }

        public void StopRecognition()
        {
            Tick();
            RecognitionRunning = false;
            ShowHint(HintResourceRecognitionStopped);
        }

        public void HideSubtitles()
        {
            Tick();
            SubtitlesVisible = false;
            ShowHint(HintResourceSubtitlesHidden);
        }

        public void ShowSubtitles()
        {
            Tick();
            SubtitlesVisible = true;
            ShowHint(HintResourceSubtitlesShown);
        }

        public void CaptureRegionSelected()
        {
            Tick();
            ShowHint(HintResourceCaptureRegionBoxed);
        }

        public void CaptureRegionSelectionCancelled()
        {
            Tick();
            ShowHint(HintResourceCaptureRegionMissing);
        }

        public void PreviewCaptureRegion(bool hasCaptureRegion)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                ShowHint(HintResourceCaptureRegionMissing);
            }
        }

        public void Refresh(bool hasCaptureRegion, bool foundText)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                ShowHint(HintResourceCaptureRegionMissing);
                return;
            }

            if (foundText)
            {
                ShowHint(HintResourceRefreshed);
            }
            else
            {
                ShowHint(HintResourceRefreshFoundNoText);
            }
        }

        public void ChangeVoiceSpeed(double speed)
        {
            Tick();
            string speedText = speed.ToString("0.##", CultureInfo.InvariantCulture);
            ShowHint(HintResourceVoiceSpeed, speedText);
        }

        public void NoteOcrMiss()
        {
            Tick();
        }

        public void NoteMatchMiss()
        {
            Tick();
        }

        public void Tick()
        {
            ExpireHintIfNeeded();
            TryStartNextOcr();
        }

        public void SetCapture(int pairIndex, OverlayRect capture)
        {
            if (pairIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pairIndex));
            }

            EnsurePairSlot(pairIndex);
            OverlayRect nextCapture = capture ?? OverlayRect.Invalid;
            OverlayRect display = _pairs[pairIndex].Display;
            if (pairIndex == 0 && nextCapture.IsValid && !display.IsValid)
            {
                display = new OverlayRect(nextCapture.X, nextCapture.Y, nextCapture.Width, nextCapture.Height);
            }

            _pairs[pairIndex] = new RegionPair(nextCapture, display);
            PersistPairs();
        }

        public void ClearCapture(int pairIndex)
        {
            if (pairIndex < 0 || pairIndex >= _pairs.Count)
            {
                return;
            }

            _pairs[pairIndex] = new RegionPair(OverlayRect.Invalid, _pairs[pairIndex].Display);
            PersistPairs();
        }

        public OverlayRect GetCapture(int pairIndex)
        {
            if (pairIndex < 0 || pairIndex >= _pairs.Count)
            {
                return OverlayRect.Invalid;
            }

            return _pairs[pairIndex].Capture;
        }

        public OverlayRect GetDisplay(int pairIndex)
        {
            if (pairIndex < 0 || pairIndex >= _pairs.Count)
            {
                return OverlayRect.Invalid;
            }

            return _pairs[pairIndex].Display;
        }

        public void Beat(params PairFrameSample[] samples)
        {
            ExpireHintIfNeeded();
            int engineCount = Math.Min(EnginePairCap, _pairs.Count);
            for (int i = 0; i < engineCount; i++)
            {
                if (!_pairs[i].Capture.IsValid)
                {
                    continue;
                }

                if (samples == null || i >= samples.Length || samples[i] == null)
                {
                    continue;
                }

                PairFrameSample sample = samples[i];
                if (sample.Empty && sample.Stable)
                {
                    _headers[i] = string.Empty;
                    _contents[i] = string.Empty;
                    continue;
                }

                if (sample.Changed && sample.Stable && !sample.Empty)
                {
                    EnqueueOcr(i);
                }
            }

            TryStartNextOcr();
        }

        public void CompleteOcr(bool miss, string content = null, string header = null)
        {
            ExpireHintIfNeeded();
            if (!_busyPairIndex.HasValue)
            {
                return;
            }

            ApplyPairResult(_busyPairIndex.Value, miss, content, header);
        }

        public void ApplyPairResult(int pairIndex, bool miss, string content = null, string header = null)
        {
            if (pairIndex < 0 || pairIndex >= _pairs.Count)
            {
                return;
            }

            if (!miss)
            {
                _headers[pairIndex] = header ?? string.Empty;
                _contents[pairIndex] = content ?? string.Empty;
                _recognitionSequence++;
                _recognitionOrders[pairIndex] = _recognitionSequence;
            }

            if (_busyPairIndex == pairIndex)
            {
                _busyPairIndex = null;
                TryStartNextOcr();
            }
        }

        public int EngineOcrIntervalMs
        {
            get { return Math.Max(EngineFloorOcrIntervalMs, _storedMs); }
        }

        public OcrIntervalSettingsView OpenOcrIntervalSettings()
        {
            return new OcrIntervalSettingsView(this, _storedMs);
        }

        internal void ApplyCommittedOcrInterval(int milliseconds)
        {
            _storedMs = milliseconds;
            _store.Write(milliseconds);
        }

        private void LoadPairs()
        {
            _pairs.Clear();
            if (_pairStore == null)
            {
                return;
            }

            IReadOnlyList<RegionPairRecord> stored = _pairStore.ReadPairs();
            if (stored != null && stored.Count > 0)
            {
                foreach (RegionPairRecord record in stored)
                {
                    _pairs.Add(FromRecord(record));
                }

                SyncPairRuntime();
                return;
            }

            List<RegionPair> migrated = MigrateLegacy(_pairStore.ReadLegacy());
            if (migrated.Count == 0)
            {
                return;
            }

            _pairs.AddRange(migrated);
            PersistPairs();
            SyncPairRuntime();
        }

        private void PersistPairs()
        {
            if (_pairStore == null)
            {
                return;
            }

            var records = new List<RegionPairRecord>(_pairs.Count);
            foreach (RegionPair pair in _pairs)
            {
                records.Add(ToRecord(pair));
            }

            _pairStore.WritePairs(records);
        }

        private static List<RegionPair> MigrateLegacy(LegacyRegionSlots legacy)
        {
            var pairs = new List<RegionPair>();
            if (legacy == null)
            {
                return pairs;
            }

            OverlayRect primaryCapture;
            bool hasPrimary = OverlayRect.TryParse(legacy.Region, out primaryCapture);
            OverlayRect secondCapture;
            bool hasSecond = OverlayRect.TryParse(legacy.Region2, out secondCapture);

            if (hasPrimary)
            {
                OverlayRect display = primaryCapture.Offset(legacy.PadHorizontal, legacy.PadVertical);
                pairs.Add(new RegionPair(primaryCapture, display));
            }
            else if (hasSecond)
            {
                pairs.Add(new RegionPair(OverlayRect.Invalid, OverlayRect.Invalid));
            }

            if (hasSecond)
            {
                pairs.Add(new RegionPair(secondCapture, OverlayRect.Invalid));
            }

            return pairs;
        }

        private static RegionPair FromRecord(RegionPairRecord record)
        {
            if (record == null)
            {
                return new RegionPair(OverlayRect.Invalid, OverlayRect.Invalid);
            }

            return new RegionPair(
                record.Capture ?? OverlayRect.Invalid,
                record.Display ?? OverlayRect.Invalid);
        }

        private static RegionPairRecord ToRecord(RegionPair pair)
        {
            return new RegionPairRecord
            {
                Capture = pair.Capture,
                Display = pair.Display
            };
        }

        private void EnsurePairSlot(int pairIndex)
        {
            while (_pairs.Count <= pairIndex)
            {
                _pairs.Add(new RegionPair(OverlayRect.Invalid, OverlayRect.Invalid));
            }

            SyncPairRuntime();
        }

        private void SyncPairRuntime()
        {
            while (_headers.Count < _pairs.Count)
            {
                _headers.Add(string.Empty);
                _contents.Add(string.Empty);
                _recognitionOrders.Add(0);
            }

            if (_headers.Count > _pairs.Count)
            {
                int extra = _headers.Count - _pairs.Count;
                _headers.RemoveRange(_pairs.Count, extra);
                _contents.RemoveRange(_pairs.Count, extra);
                _recognitionOrders.RemoveRange(_pairs.Count, extra);
            }
        }

        private void EnqueueOcr(int pairIndex)
        {
            if (_busyPairIndex == pairIndex)
            {
                return;
            }

            if (_ocrQueue.Contains(pairIndex))
            {
                return;
            }

            _ocrQueue.Add(pairIndex);
        }

        private void TryStartNextOcr()
        {
            if (_busyPairIndex.HasValue || _ocrQueue.Count == 0)
            {
                return;
            }

            if (!TryBeginOcr())
            {
                return;
            }

            _busyPairIndex = _ocrQueue[0];
            _ocrQueue.RemoveAt(0);
        }

        public bool TryBeginOcr()
        {
            if (_utcNow() - _lastOcrTime < TimeSpan.FromMilliseconds(EngineOcrIntervalMs))
            {
                return false;
            }

            _lastOcrTime = _utcNow();
            return true;
        }

        public void ResetOcrInterval()
        {
            _lastOcrTime = DateTime.MinValue;
        }

        private void ExpireHintIfNeeded()
        {
            if (!HintVisible || !_hintExpiresAt.HasValue)
            {
                return;
            }

            if (_utcNow() >= _hintExpiresAt.Value)
            {
                ClearHint();
            }
        }

        private void ShowHint(string resourceKey, params object[] formatArguments)
        {
            HintResourceKey = resourceKey;
            HintFormatArguments = formatArguments == null || formatArguments.Length == 0
                ? null
                : formatArguments;
            HintVisible = true;
            _hintExpiresAt = _utcNow().AddMilliseconds(HintDurationMs);
            HintChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ClearHint()
        {
            HintVisible = false;
            HintResourceKey = null;
            HintFormatArguments = null;
            _hintExpiresAt = null;
            HintChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public sealed class OcrIntervalSettingsView
    {
        private readonly LiveOverlaySession _session;
        private int _rawMs;

        internal OcrIntervalSettingsView(LiveOverlaySession session, int rawMs)
        {
            _session = session;
            Show(rawMs);
        }

        public string BoxText { get; set; }

        public bool IsOutOfRange { get; private set; }

        public void Commit()
        {
            if (!int.TryParse(BoxText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                Show(_rawMs);
                return;
            }

            int clamped = parsed;
            if (clamped < LiveOverlaySession.UiMinOcrIntervalMs)
            {
                clamped = LiveOverlaySession.UiMinOcrIntervalMs;
            }
            else if (clamped > LiveOverlaySession.UiMaxOcrIntervalMs)
            {
                clamped = LiveOverlaySession.UiMaxOcrIntervalMs;
            }

            _session.ApplyCommittedOcrInterval(clamped);
            Show(clamped);
        }

        private void Show(int rawMs)
        {
            _rawMs = rawMs;
            BoxText = rawMs.ToString(CultureInfo.InvariantCulture);
            IsOutOfRange = rawMs < LiveOverlaySession.UiMinOcrIntervalMs
                || rawMs > LiveOverlaySession.UiMaxOcrIntervalMs;
        }
    }
}
