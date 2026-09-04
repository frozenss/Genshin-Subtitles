using System;
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

        private const string HintResourceRecognitionRunning = "Hint_RecognitionRunning";
        private const string HintResourceRecognitionStopped = "Hint_RecognitionStopped";
        private const string HintResourceCaptureRegionBoxed = "Hint_CaptureRegionBoxed";
        private const string HintResourceSubtitlesHidden = "Hint_SubtitlesHidden";
        private const string HintResourceSubtitlesShown = "Hint_SubtitlesShown";
        private const string HintResourceRefreshed = "Hint_Refreshed";
        private const string HintResourceRefreshFoundNoText = "Hint_RefreshFoundNoText";
        private const string HintResourceVoiceSpeed = "Hint_VoiceSpeed";
        private const string HintResourceCaptureRegionMissing = "Hint_CaptureRegionMissing";

        private const string CopyRecognitionRunning = "识别中";
        private const string CopyRecognitionStopped = "已停止";
        private const string CopyCaptureRegionBoxed = "已选识别区";
        private const string CopySubtitlesHidden = "字幕已隐藏";
        private const string CopySubtitlesShown = "字幕已显示";
        private const string CopyRefreshed = "已刷新";
        private const string CopyRefreshFoundNoText = "未识别到文本";
        private const string CopyCaptureRegionMissing = "未设置识别区";

        private readonly IOcrIntervalStore _store;
        private readonly Func<DateTime> _utcNow;
        private int _storedMs;
        private DateTime? _hintExpiresAt;

        public LiveOverlaySession(IOcrIntervalStore store)
            : this(store, null)
        {
        }

        public LiveOverlaySession(IOcrIntervalStore store, Func<DateTime> utcNow)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _storedMs = _store.Read(DefaultOcrIntervalMs);
            SubtitlesVisible = true;
        }

        public event EventHandler HintChanged;

        public bool HintVisible { get; private set; }

        public string HintText { get; private set; }

        public string HintResourceKey { get; private set; }

        public object[] HintFormatArguments { get; private set; }

        public bool RecognitionRunning { get; private set; }

        public bool SubtitlesVisible { get; private set; }

        public void StartRecognition(bool hasCaptureRegion)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                ShowHint(CopyCaptureRegionMissing, HintResourceCaptureRegionMissing);
                return;
            }

            RecognitionRunning = true;
            ShowHint(CopyRecognitionRunning, HintResourceRecognitionRunning);
        }

        public void StopRecognition()
        {
            Tick();
            RecognitionRunning = false;
            ShowHint(CopyRecognitionStopped, HintResourceRecognitionStopped);
        }

        public void HideSubtitles()
        {
            Tick();
            SubtitlesVisible = false;
            ShowHint(CopySubtitlesHidden, HintResourceSubtitlesHidden);
        }

        public void ShowSubtitles()
        {
            Tick();
            SubtitlesVisible = true;
            ShowHint(CopySubtitlesShown, HintResourceSubtitlesShown);
        }

        public void CaptureRegionSelected()
        {
            Tick();
            ShowHint(CopyCaptureRegionBoxed, HintResourceCaptureRegionBoxed);
        }

        public void CaptureRegionSelectionCancelled()
        {
            Tick();
            ShowHint(CopyCaptureRegionMissing, HintResourceCaptureRegionMissing);
        }

        public void PreviewCaptureRegion(bool hasCaptureRegion)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                ShowHint(CopyCaptureRegionMissing, HintResourceCaptureRegionMissing);
            }
        }

        public void Refresh(bool hasCaptureRegion, bool foundText)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                ShowHint(CopyCaptureRegionMissing, HintResourceCaptureRegionMissing);
                return;
            }

            if (foundText)
            {
                ShowHint(CopyRefreshed, HintResourceRefreshed);
            }
            else
            {
                ShowHint(CopyRefreshFoundNoText, HintResourceRefreshFoundNoText);
            }
        }

        public void ChangeVoiceSpeed(double speed)
        {
            Tick();
            string speedText = speed.ToString("0.##", CultureInfo.InvariantCulture);
            ShowHint(
                "倍速 " + speedText + "×",
                HintResourceVoiceSpeed,
                speedText);
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
            if (!HintVisible || !_hintExpiresAt.HasValue)
            {
                return;
            }

            if (_utcNow() >= _hintExpiresAt.Value)
            {
                ClearHint();
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

        private void ShowHint(string text, string resourceKey, params object[] formatArguments)
        {
            HintText = text;
            HintResourceKey = resourceKey;
            HintFormatArguments = formatArguments;
            HintVisible = true;
            _hintExpiresAt = _utcNow().AddMilliseconds(HintDurationMs);
            HintChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ClearHint()
        {
            HintVisible = false;
            HintText = null;
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

        public string OutOfRangeWarning { get; private set; }

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
            bool outOfRange = rawMs < LiveOverlaySession.UiMinOcrIntervalMs
                || rawMs > LiveOverlaySession.UiMaxOcrIntervalMs;
            OutOfRangeWarning = outOfRange
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "当前 {0}ms，超出 200–1000。引擎仍按此值跑。改动后会夹进范围。",
                    rawMs)
                : null;
        }
    }
}
