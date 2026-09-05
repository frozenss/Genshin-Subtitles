using System;
using System.Collections.Generic;
using System.Globalization;

namespace GI_Subtitles.Core.Overlay
{
    public sealed partial class LiveOverlaySession
    {
        public const int DefaultOcrIntervalMs = 400;
        public const int UiMinOcrIntervalMs = 200;
        public const int UiMaxOcrIntervalMs = 1000;
        public const int EngineFloorOcrIntervalMs = 1;
        public const string OcrIntervalConfigKey = "OCRInterval";
        public const int PreviewDurationMs = 10000;
        public const int DialogueChoiceEchoDurationMs = 3000;
        public const int DarkScreenScanIntervalMs = 500;
        public const int DialogueOptionScanIntervalMs = 400;
        public const int DarkScreenOcrSlot = -2;
        public const int DialogueOptionsOcrSlot = -1;
        public const int EnginePairCap = 8;
        public const int SettingsPairCap = 4;
        private const string DialogueChoiceEchoPrefix = "◆ ";

        private readonly IOcrIntervalStore _store;
        private readonly IRegionPairStore _pairStore;
        private readonly Func<DateTime> _utcNow;
        private readonly List<RegionPair> _pairs = new List<RegionPair>();
        private readonly List<string> _headers = new List<string>();
        private readonly List<string> _contents = new List<string>();
        private readonly List<int> _recognitionOrders = new List<int>();
        private readonly List<int> _ocrQueue = new List<int>();
        private readonly List<RegionOutline> _previewOutlines = new List<RegionOutline>();
        private readonly List<RegionOutline> _adjustOutlines = new List<RegionOutline>();
        private int _storedMs;
        private DateTime? _previewExpiresAt;
        private DateTime? _echoExpiresAt;
        private OverlayRect _darkScreenBand = OverlayRect.Invalid;
        private OverlayRect _darkScreenDisplay = OverlayRect.Invalid;
        private OverlayRect _dialogueOptionDisplay = OverlayRect.Invalid;
        private string _darkScreenHeader = string.Empty;
        private string _darkScreenContent = string.Empty;
        private int _darkScreenRecognitionOrder;
        private bool _darkScreenActive;
        private string _echoContent = string.Empty;
        private int _echoRecognitionOrder;
        private DateTime _lastOcrTime = DateTime.MinValue;
        private int? _busyPairIndex;
        private int _recognitionSequence;
        private int _nextPairId = 1;
        private OverlayRect _addCapture = OverlayRect.Invalid;
        private OverlayRect _addDisplay = OverlayRect.Invalid;
        private VoicePlayRequest _pendingVoicePlay;

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

        public int? BusyOcrSlot
        {
            get { return _busyPairIndex; }
        }

        public int? BusyOcrPairIndex
        {
            get
            {
                if (!_busyPairIndex.HasValue || _busyPairIndex.Value < 0)
                {
                    return null;
                }

                return _busyPairIndex;
            }
        }

        public ExtraPathBody DarkScreenBody
        {
            get
            {
                OverlayRect display = ResolveDarkScreenDisplay();
                bool visible = SubtitlesVisible
                    && display.IsValid
                    && !string.IsNullOrEmpty(_darkScreenContent);
                return new ExtraPathBody(
                    display,
                    _darkScreenHeader,
                    _darkScreenContent,
                    visible,
                    _darkScreenRecognitionOrder);
            }
        }

        public OverlayRect DarkScreenDisplay
        {
            get { return _darkScreenDisplay; }
        }

        public OverlayRect DialogueOptionDisplay
        {
            get { return _dialogueOptionDisplay; }
        }

        public ExtraPathBody DialogueChoiceEcho
        {
            get
            {
                OverlayRect display = ResolveEchoDisplay();
                bool visible = SubtitlesVisible
                    && display.IsValid
                    && !string.IsNullOrEmpty(_echoContent);
                return new ExtraPathBody(
                    display,
                    string.Empty,
                    _echoContent,
                    visible,
                    _echoRecognitionOrder,
                    followsVoicePrimary: !_dialogueOptionDisplay.IsValid);
            }
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

        public event EventHandler PreviewChanged;

        public event EventHandler AdjustChanged;

        public IReadOnlyList<RegionOutline> PreviewOutlines
        {
            get { return _previewOutlines; }
        }

        public IReadOnlyList<RegionOutline> AdjustOutlines
        {
            get { return _adjustOutlines; }
        }

        public OverlayAdjustTarget ArmedTarget { get; private set; }

        public int ArmedPairId { get; private set; }

        public bool IsClickThrough
        {
            get { return ArmedTarget == OverlayAdjustTarget.None; }
        }

        public int ArmedPairIndex
        {
            get { return IndexOfPair(ArmedPairId); }
        }

        public bool RecognitionRunning { get; private set; }

        public bool SubtitlesVisible { get; private set; }

        public int VoicePrimaryId { get; private set; }

        public bool AddInProgress { get; private set; }

        public bool VoicePlaybackActive { get; private set; }

        public int VoicePlaybackToken { get; private set; }

        public void PreviewCaptureRegion(bool hasCaptureRegion)
        {
            PreviewCaptureRegion(hasCaptureRegion, darkScreenScanOn: false);
        }

        public void PreviewCaptureRegion(bool hasCaptureRegion, bool darkScreenScanOn)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                WriteOperatorAction(OperatorJob.Preview, null, HintResourceCaptureRegionMissing);
                ClearPreview();
                return;
            }

            BuildPreviewOutlines(darkScreenScanOn);
            _previewExpiresAt = _utcNow().AddMilliseconds(PreviewDurationMs);
            PreviewChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool TryToggleDisplayAdjust(int pairId)
        {
            Tick();
            int index = IndexOfPair(pairId);
            if (index < 0 || !_pairs[index].Display.IsValid)
            {
                return false;
            }

            if (ArmedTarget == OverlayAdjustTarget.Pair && ArmedPairId == pairId)
            {
                ClearArm();
                return true;
            }

            Arm(OverlayAdjustTarget.Pair, pairId);
            return true;
        }

        public bool TryToggleDarkScreenDisplayAdjust()
        {
            Tick();
            if (!_darkScreenDisplay.IsValid)
            {
                return false;
            }

            if (ArmedTarget == OverlayAdjustTarget.DarkScreenDisplay)
            {
                ClearArm();
                return true;
            }

            Arm(OverlayAdjustTarget.DarkScreenDisplay, 0);
            return true;
        }

        public bool TryToggleDialogueOptionDisplayAdjust()
        {
            Tick();
            if (!_dialogueOptionDisplay.IsValid)
            {
                return false;
            }

            if (ArmedTarget == OverlayAdjustTarget.DialogueOptionDisplay)
            {
                ClearArm();
                return true;
            }

            Arm(OverlayAdjustTarget.DialogueOptionDisplay, 0);
            return true;
        }

        public void CancelDisplayAdjust()
        {
            Tick();
            ClearArm();
        }

        public void Tick()
        {
            ExpireHintIfNeeded();
            ExpirePreviewIfNeeded();
            ExpireEchoIfNeeded();
            TryStartNextOcr();
        }

        public void NoteVoicePlaybackEnded()
        {
            VoicePlaybackActive = false;
            ClearPendingVoiceLog();
        }

        public void SetCapture(int pairIndex, OverlayRect capture)
        {
            if (pairIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pairIndex));
            }

            EnsurePairSlot(pairIndex);
            OverlayRect nextCapture = capture ?? OverlayRect.Invalid;
            _pairs[pairIndex] = new RegionPair(_pairs[pairIndex].Id, nextCapture, _pairs[pairIndex].Display);
            PersistPairs();
            if (ArmedPairId == _pairs[pairIndex].Id)
            {
                RebuildAdjustOutlines();
            }
        }

        public void SetDisplay(int pairIndex, OverlayRect display)
        {
            if (pairIndex < 0 || pairIndex >= _pairs.Count)
            {
                return;
            }

            OverlayRect nextDisplay = display ?? OverlayRect.Invalid;
            RegionPair current = _pairs[pairIndex];
            _pairs[pairIndex] = new RegionPair(current.Id, current.Capture, nextDisplay);
            PersistPairs();
            if (ArmedPairId == current.Id)
            {
                if (!nextDisplay.IsValid)
                {
                    ClearArm();
                }
                else
                {
                    RebuildAdjustOutlines();
                }
            }
        }

        public bool TryStartAdd()
        {
            if (AddInProgress || _pairs.Count >= SettingsPairCap)
            {
                return false;
            }

            AddInProgress = true;
            _addCapture = OverlayRect.Invalid;
            _addDisplay = OverlayRect.Invalid;
            return true;
        }

        public void SetAddCapture(OverlayRect capture)
        {
            if (!AddInProgress)
            {
                return;
            }

            _addCapture = capture ?? OverlayRect.Invalid;
        }

        public void SetAddDisplay(OverlayRect display)
        {
            if (!AddInProgress)
            {
                return;
            }

            _addDisplay = display ?? OverlayRect.Invalid;
        }

        public void AbortAdd()
        {
            ClearAddState();
        }

        public bool TryCommitAdd()
        {
            if (!AddInProgress || _pairs.Count >= SettingsPairCap)
            {
                return false;
            }

            int id = AllocateId();
            _pairs.Add(new RegionPair(id, _addCapture, _addDisplay));
            if (VoicePrimaryId == 0)
            {
                VoicePrimaryId = id;
            }

            SyncPairRuntime();
            ClearAddState();
            PersistPairs();
            return true;
        }

        public void DeletePair(int id)
        {
            int index = IndexOfPair(id);
            if (index < 0)
            {
                return;
            }

            bool wasPrimary = VoicePrimaryId == id;
            bool wasArmed = ArmedPairId == id;
            RemovePairAt(index);
            if (wasPrimary)
            {
                VoicePrimaryId = _pairs.Count == 0 ? 0 : _pairs[0].Id;
            }

            PersistPairs();
            if (wasArmed)
            {
                ClearArm();
            }
            else if (ArmedPairId != 0)
            {
                RebuildAdjustOutlines();
                AdjustChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetVoicePrimary(int id)
        {
            if (IndexOfPair(id) < 0)
            {
                return;
            }

            VoicePrimaryId = id;
            PersistPairs();
        }

        public void SetDarkScreenDisplay(OverlayRect display)
        {
            _darkScreenDisplay = display ?? OverlayRect.Invalid;
            PersistExtraPathDisplays();
            RefreshArmedExtraPath(OverlayAdjustTarget.DarkScreenDisplay, _darkScreenDisplay);
        }

        public void ClearDarkScreenDisplay()
        {
            SetDarkScreenDisplay(OverlayRect.Invalid);
        }

        public void SetDialogueOptionDisplay(OverlayRect display)
        {
            _dialogueOptionDisplay = display ?? OverlayRect.Invalid;
            PersistExtraPathDisplays();
            RefreshArmedExtraPath(OverlayAdjustTarget.DialogueOptionDisplay, _dialogueOptionDisplay);
        }

        public void ClearDialogueOptionDisplay()
        {
            SetDialogueOptionDisplay(OverlayRect.Invalid);
        }

        public bool TryGetVoicePrimaryCapture(out int pairIndex, out OverlayRect capture)
        {
            pairIndex = IndexOfPair(VoicePrimaryId);
            if (pairIndex < 0)
            {
                capture = OverlayRect.Invalid;
                return false;
            }

            capture = _pairs[pairIndex].Capture;
            return capture.IsValid;
        }

        public VoicePlayRequest TakeVoicePlayRequest()
        {
            VoicePlayRequest request = _pendingVoicePlay;
            _pendingVoicePlay = null;
            return request;
        }

        public void ClearCapture(int pairIndex)
        {
            if (pairIndex < 0 || pairIndex >= _pairs.Count)
            {
                return;
            }

            _pairs[pairIndex] = new RegionPair(_pairs[pairIndex].Id, OverlayRect.Invalid, _pairs[pairIndex].Display);
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
            Beat(ExtraPathSample.None, samples);
        }

        public void Beat(ExtraPathSample extra, params PairFrameSample[] samples)
        {
            ExpireHintIfNeeded();
            ExpirePreviewIfNeeded();
            ExpireEchoIfNeeded();
            ApplyExtraPathSample(extra);
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

        public void CompleteOcr(
            bool miss,
            string content = null,
            string header = null,
            string ocrText = null,
            string original = null,
            bool matchMiss = false)
        {
            ExpireHintIfNeeded();
            ExpirePreviewIfNeeded();
            ExpireEchoIfNeeded();
            if (!_busyPairIndex.HasValue)
            {
                return;
            }

            int busy = _busyPairIndex.Value;
            WritePipelineForSlot(busy, miss, content, ocrText, original, matchMiss);
            if (busy == DarkScreenOcrSlot)
            {
                ApplyDarkScreenResult(miss, content, header);
                return;
            }

            if (busy == DialogueOptionsOcrSlot)
            {
                ReleaseBusySlot();
                return;
            }

            ApplyPairResult(busy, miss, content, header);
        }

        public void ApplyPairResult(
            int pairIndex,
            bool miss,
            string content = null,
            string header = null,
            string ocrText = null,
            string original = null,
            bool matchMiss = false)
        {
            if (pairIndex < 0 || pairIndex >= _pairs.Count)
            {
                return;
            }

            if (_busyPairIndex != pairIndex)
            {
                WritePipelineResult(
                    ActivityLogScope.Pair,
                    pairIndex + 1,
                    _pairs[pairIndex].Id == VoicePrimaryId,
                    miss,
                    content,
                    ocrText,
                    original,
                    matchMiss);
            }

            if (!miss)
            {
                _headers[pairIndex] = header ?? string.Empty;
                _contents[pairIndex] = content ?? string.Empty;
                _recognitionSequence++;
                _recognitionOrders[pairIndex] = _recognitionSequence;
                if (_pairs[pairIndex].Id == VoicePrimaryId)
                {
                    EmitVoicePlayRequest(pairIndex);
                }
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
                _nextPairId = 1;
                VoicePrimaryId = 0;
                return;
            }

            _nextPairId = _pairStore.ReadNextPairId();
            VoicePrimaryId = _pairStore.ReadVoicePrimaryId();

            IReadOnlyList<RegionPairRecord> stored = _pairStore.ReadPairs();
            bool wroteLegacy = false;
            if (stored != null && stored.Count > 0)
            {
                foreach (RegionPairRecord record in stored)
                {
                    _pairs.Add(FromRecord(record));
                }
            }
            else
            {
                List<RegionPair> migrated = MigrateLegacy(_pairStore.ReadLegacy());
                if (migrated.Count > 0)
                {
                    _pairs.AddRange(migrated);
                    wroteLegacy = true;
                }
            }

            SyncPairRuntime();
            bool identitiesChanged = EnsureIdentities();
            LoadExtraPathDisplays();
            if (wroteLegacy || identitiesChanged)
            {
                PersistPairs();
            }
        }

        private void LoadExtraPathDisplays()
        {
            _darkScreenDisplay = OverlayRect.Invalid;
            _dialogueOptionDisplay = OverlayRect.Invalid;
            if (_pairStore == null)
            {
                return;
            }

            OverlayRect darkScreen = _pairStore.ReadDarkScreenDisplay();
            _darkScreenDisplay = darkScreen ?? OverlayRect.Invalid;
            OverlayRect dialogueOption = _pairStore.ReadDialogueOptionDisplay();
            _dialogueOptionDisplay = dialogueOption ?? OverlayRect.Invalid;
        }

        private void PersistExtraPathDisplays()
        {
            if (_pairStore == null)
            {
                return;
            }

            _pairStore.WriteDarkScreenDisplay(_darkScreenDisplay);
            _pairStore.WriteDialogueOptionDisplay(_dialogueOptionDisplay);
        }

        private OverlayRect ResolveDarkScreenDisplay()
        {
            if (_darkScreenDisplay.IsValid)
            {
                return _darkScreenDisplay;
            }

            return _darkScreenBand;
        }

        private OverlayRect ResolveEchoDisplay()
        {
            if (_dialogueOptionDisplay.IsValid)
            {
                return _dialogueOptionDisplay;
            }

            int index = IndexOfPair(VoicePrimaryId);
            if (index < 0)
            {
                return OverlayRect.Invalid;
            }

            return _pairs[index].Display;
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
            _pairStore.WriteVoicePrimaryId(VoicePrimaryId);
            _pairStore.WriteNextPairId(_nextPairId);
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
                pairs.Add(new RegionPair(0, primaryCapture, display));
            }
            else if (hasSecond)
            {
                pairs.Add(new RegionPair(0, OverlayRect.Invalid, OverlayRect.Invalid));
            }

            if (hasSecond)
            {
                pairs.Add(new RegionPair(0, secondCapture, OverlayRect.Invalid));
            }

            return pairs;
        }

        private static RegionPair FromRecord(RegionPairRecord record)
        {
            if (record == null)
            {
                return new RegionPair(0, OverlayRect.Invalid, OverlayRect.Invalid);
            }

            return new RegionPair(
                record.Id,
                record.Capture ?? OverlayRect.Invalid,
                record.Display ?? OverlayRect.Invalid);
        }

        private static RegionPairRecord ToRecord(RegionPair pair)
        {
            return new RegionPairRecord
            {
                Id = pair.Id,
                Capture = pair.Capture,
                Display = pair.Display
            };
        }

        private void EnsurePairSlot(int pairIndex)
        {
            while (_pairs.Count <= pairIndex)
            {
                int id = AllocateId();
                _pairs.Add(new RegionPair(id, OverlayRect.Invalid, OverlayRect.Invalid));
                if (VoicePrimaryId == 0)
                {
                    VoicePrimaryId = id;
                }
            }

            SyncPairRuntime();
        }

        private bool EnsureIdentities()
        {
            bool dirty = false;
            int maxId = 0;
            for (int i = 0; i < _pairs.Count; i++)
            {
                if (_pairs[i].Id > maxId)
                {
                    maxId = _pairs[i].Id;
                }
            }

            if (_nextPairId < maxId + 1)
            {
                _nextPairId = maxId + 1;
                if (maxId > 0)
                {
                    dirty = true;
                }
            }

            if (_nextPairId < 1)
            {
                _nextPairId = 1;
            }

            for (int i = 0; i < _pairs.Count; i++)
            {
                if (_pairs[i].Id > 0)
                {
                    continue;
                }

                RegionPair current = _pairs[i];
                _pairs[i] = new RegionPair(AllocateId(), current.Capture, current.Display);
                dirty = true;
            }

            if (VoicePrimaryId != 0 && IndexOfPair(VoicePrimaryId) < 0)
            {
                VoicePrimaryId = _pairs.Count == 0 ? 0 : _pairs[0].Id;
                dirty = true;
            }

            if (VoicePrimaryId == 0 && _pairs.Count > 0)
            {
                VoicePrimaryId = _pairs[0].Id;
                dirty = true;
            }

            return dirty;
        }

        private int AllocateId()
        {
            if (_nextPairId < 1)
            {
                _nextPairId = 1;
            }

            int id = _nextPairId;
            _nextPairId++;
            return id;
        }

        private int IndexOfPair(int id)
        {
            if (id <= 0)
            {
                return -1;
            }

            for (int i = 0; i < _pairs.Count; i++)
            {
                if (_pairs[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ClearAddState()
        {
            AddInProgress = false;
            _addCapture = OverlayRect.Invalid;
            _addDisplay = OverlayRect.Invalid;
        }

        private void RemovePairAt(int index)
        {
            _pairs.RemoveAt(index);
            if (index < _headers.Count)
            {
                _headers.RemoveAt(index);
                _contents.RemoveAt(index);
                _recognitionOrders.RemoveAt(index);
            }

            if (_busyPairIndex.HasValue)
            {
                if (_busyPairIndex.Value == index)
                {
                    _busyPairIndex = null;
                }
                else if (_busyPairIndex.Value > index)
                {
                    _busyPairIndex = _busyPairIndex.Value - 1;
                }
            }

            for (int i = _ocrQueue.Count - 1; i >= 0; i--)
            {
                if (_ocrQueue[i] == index)
                {
                    _ocrQueue.RemoveAt(i);
                }
                else if (_ocrQueue[i] > index)
                {
                    _ocrQueue[i]--;
                }
            }
        }

        private void EmitVoicePlayRequest(int pairIndex)
        {
            VoicePlaybackToken++;
            VoicePlaybackActive = true;
            _pendingVoicePlay = new VoicePlayRequest(
                _pairs[pairIndex].Id,
                _headers[pairIndex],
                _contents[pairIndex],
                VoicePlaybackToken);
            RememberVoiceLogRow();
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

        private void ApplyExtraPathSample(ExtraPathSample extra)
        {
            if (extra == null || extra == ExtraPathSample.None)
            {
                return;
            }

            if (extra.DialogueChoiceSelected)
            {
                ShowDialogueChoiceEcho(extra.DialogueChoiceContent);
            }

            if (!HasValidCapture)
            {
                return;
            }

            int insertAt = 0;
            if (extra.DarkScreenObserved)
            {
                if (!extra.DarkScreenIsDark || !extra.DarkScreenHasCandidate)
                {
                    ClearDarkScreen();
                    RemoveQueuedSlot(DarkScreenOcrSlot);
                }
                else
                {
                    _darkScreenActive = true;
                    _darkScreenBand = extra.DarkScreenBand ?? OverlayRect.Invalid;
                    if (extra.DarkScreenNeedsOcr)
                    {
                        insertAt = EnqueueOcrAt(insertAt, DarkScreenOcrSlot);
                    }
                }
            }

            if (extra.DialogueOptionsNeedOcr)
            {
                insertAt = EnqueueOcrAt(insertAt, DialogueOptionsOcrSlot);
            }
        }

        private void ShowDialogueChoiceEcho(string content)
        {
            string trimmed = content ?? string.Empty;
            string echo = string.Empty;
            if (!string.IsNullOrEmpty(trimmed))
            {
                echo = trimmed.StartsWith(DialogueChoiceEchoPrefix, StringComparison.Ordinal)
                    ? trimmed
                    : DialogueChoiceEchoPrefix + trimmed;
                WriteDialogueChoiceRow(echo);
            }

            if (!HasValidCapture || !ResolveEchoDisplay().IsValid || string.IsNullOrEmpty(trimmed))
            {
                ClearEcho();
                return;
            }

            _echoContent = echo;
            _recognitionSequence++;
            _echoRecognitionOrder = _recognitionSequence;
            _echoExpiresAt = _utcNow().AddMilliseconds(DialogueChoiceEchoDurationMs);
            EmitExtraPathVoice(string.Empty, _echoContent);
        }

        private void ApplyDarkScreenResult(bool miss, string content, string header)
        {
            if (!miss && _darkScreenActive)
            {
                _darkScreenHeader = header ?? string.Empty;
                _darkScreenContent = content ?? string.Empty;
                _recognitionSequence++;
                _darkScreenRecognitionOrder = _recognitionSequence;
                EmitExtraPathVoice(_darkScreenHeader, _darkScreenContent);
            }

            ReleaseBusySlot();
        }

        private void ClearDarkScreen()
        {
            _darkScreenActive = false;
            _darkScreenBand = OverlayRect.Invalid;
            _darkScreenHeader = string.Empty;
            _darkScreenContent = string.Empty;
            _darkScreenRecognitionOrder = 0;
        }

        private void ClearEcho()
        {
            _echoContent = string.Empty;
            _echoRecognitionOrder = 0;
            _echoExpiresAt = null;
        }

        private void ExpireEchoIfNeeded()
        {
            if (_echoExpiresAt.HasValue && _utcNow() >= _echoExpiresAt.Value)
            {
                ClearEcho();
            }
        }

        private void EmitExtraPathVoice(string header, string content)
        {
            if (VoicePlaybackActive || string.IsNullOrEmpty(content))
            {
                return;
            }

            VoicePlaybackToken++;
            VoicePlaybackActive = true;
            _pendingVoicePlay = new VoicePlayRequest(0, header, content, VoicePlaybackToken, extraPath: true);
            RememberVoiceLogRow();
        }

        private void ReleaseBusySlot()
        {
            _busyPairIndex = null;
            TryStartNextOcr();
        }

        private void EnqueueOcr(int pairIndex)
        {
            EnqueueOcrAt(_ocrQueue.Count, pairIndex);
        }

        private int EnqueueOcrAt(int insertAt, int slot)
        {
            if (_busyPairIndex == slot)
            {
                return insertAt;
            }

            int existing = _ocrQueue.IndexOf(slot);
            if (existing >= 0)
            {
                if (existing != insertAt)
                {
                    _ocrQueue.RemoveAt(existing);
                    if (existing < insertAt)
                    {
                        insertAt--;
                    }

                    _ocrQueue.Insert(Math.Min(insertAt, _ocrQueue.Count), slot);
                }

                return insertAt + 1;
            }

            if (insertAt < 0)
            {
                insertAt = 0;
            }

            if (insertAt >= _ocrQueue.Count)
            {
                _ocrQueue.Add(slot);
            }
            else
            {
                _ocrQueue.Insert(insertAt, slot);
            }

            return insertAt + 1;
        }

        private void RemoveQueuedSlot(int slot)
        {
            for (int i = _ocrQueue.Count - 1; i >= 0; i--)
            {
                if (_ocrQueue[i] == slot)
                {
                    _ocrQueue.RemoveAt(i);
                }
            }
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

        private void ExpirePreviewIfNeeded()
        {
            if (_previewExpiresAt.HasValue && _utcNow() >= _previewExpiresAt.Value)
            {
                ClearPreview();
            }
        }

        private void BuildPreviewOutlines(bool darkScreenScanOn)
        {
            _previewOutlines.Clear();
            for (int i = 0; i < _pairs.Count; i++)
            {
                AddPairOutlines(_previewOutlines, i);
            }

            if (_darkScreenDisplay.IsValid)
            {
                _previewOutlines.Add(new RegionOutline(
                    0,
                    _darkScreenDisplay,
                    true,
                    RegionOutlineKind.DarkScreenDisplay));
            }
            else if (darkScreenScanOn && _darkScreenActive && _darkScreenBand.IsValid)
            {
                _previewOutlines.Add(new RegionOutline(
                    0,
                    _darkScreenBand,
                    false,
                    RegionOutlineKind.DarkScreenCandidate));
            }

            if (_dialogueOptionDisplay.IsValid)
            {
                _previewOutlines.Add(new RegionOutline(
                    0,
                    _dialogueOptionDisplay,
                    true,
                    RegionOutlineKind.DialogueOptionDisplay));
            }
        }

        private void AddPairOutlines(List<RegionOutline> outlines, int pairIndex)
        {
            RegionPair pair = _pairs[pairIndex];
            int ordinal = pairIndex + 1;
            if (pair.Capture.IsValid)
            {
                outlines.Add(new RegionOutline(ordinal, pair.Capture, false));
            }

            if (pair.Display.IsValid)
            {
                outlines.Add(new RegionOutline(ordinal, pair.Display, true));
            }
        }

        private void ClearPreview()
        {
            if (_previewOutlines.Count == 0 && !_previewExpiresAt.HasValue)
            {
                return;
            }

            _previewOutlines.Clear();
            _previewExpiresAt = null;
            PreviewChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RebuildAdjustOutlines()
        {
            _adjustOutlines.Clear();
            if (ArmedTarget == OverlayAdjustTarget.Pair)
            {
                int index = IndexOfPair(ArmedPairId);
                if (index >= 0)
                {
                    AddPairOutlines(_adjustOutlines, index);
                }

                return;
            }

            if (ArmedTarget == OverlayAdjustTarget.DarkScreenDisplay && _darkScreenDisplay.IsValid)
            {
                _adjustOutlines.Add(new RegionOutline(
                    0,
                    _darkScreenDisplay,
                    true,
                    RegionOutlineKind.DarkScreenDisplay));
                return;
            }

            if (ArmedTarget == OverlayAdjustTarget.DialogueOptionDisplay && _dialogueOptionDisplay.IsValid)
            {
                _adjustOutlines.Add(new RegionOutline(
                    0,
                    _dialogueOptionDisplay,
                    true,
                    RegionOutlineKind.DialogueOptionDisplay));
            }
        }

        private void Arm(OverlayAdjustTarget target, int pairId)
        {
            ArmedTarget = target;
            ArmedPairId = pairId;
            RebuildAdjustOutlines();
            AdjustChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RefreshArmedExtraPath(OverlayAdjustTarget target, OverlayRect display)
        {
            if (ArmedTarget != target)
            {
                return;
            }

            if (!display.IsValid)
            {
                ClearArm();
                return;
            }

            RebuildAdjustOutlines();
        }

        private void ClearArm()
        {
            if (ArmedTarget == OverlayAdjustTarget.None && ArmedPairId == 0 && _adjustOutlines.Count == 0)
            {
                return;
            }

            ArmedTarget = OverlayAdjustTarget.None;
            ArmedPairId = 0;
            _adjustOutlines.Clear();
            AdjustChanged?.Invoke(this, EventArgs.Empty);
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

    public sealed class VoicePlayRequest
    {
        public VoicePlayRequest(int pairId, string header, string content, int token)
            : this(pairId, header, content, token, extraPath: false)
        {
        }

        public VoicePlayRequest(int pairId, string header, string content, int token, bool extraPath)
        {
            PairId = pairId;
            Header = header ?? string.Empty;
            Content = content ?? string.Empty;
            Token = token;
            ExtraPath = extraPath;
        }

        public int PairId { get; }

        public string Header { get; }

        public string Content { get; }

        public int Token { get; }

        public bool ExtraPath { get; }
    }
}
