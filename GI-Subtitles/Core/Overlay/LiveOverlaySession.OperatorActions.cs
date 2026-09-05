using System;
using System.Collections.Generic;
using System.Globalization;

namespace GI_Subtitles.Core.Overlay
{
    public sealed partial class LiveOverlaySession
    {
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

        private readonly List<ActivityLogRow> _activityLog = new List<ActivityLogRow>();
        private DateTime? _hintExpiresAt;

        public event EventHandler HintChanged;

        public event EventHandler ActivityLogChanged;

        public IReadOnlyList<ActivityLogRow> ActivityLog
        {
            get { return _activityLog; }
        }

        public bool HintVisible { get; private set; }

        public string HintResourceKey { get; private set; }

        public object[] HintFormatArguments { get; private set; }

        public void StartRecognition(bool hasCaptureRegion)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                WriteOperatorAction(OperatorJob.StartRecognition, null, HintResourceCaptureRegionMissing);
                return;
            }

            RecognitionRunning = true;
            WriteOperatorAction(OperatorJob.StartRecognition, null, HintResourceRecognitionRunning);
        }

        public void StopRecognition()
        {
            Tick();
            RecognitionRunning = false;
            WriteOperatorAction(OperatorJob.StopRecognition, null, HintResourceRecognitionStopped);
        }

        public void HideSubtitles()
        {
            Tick();
            SubtitlesVisible = false;
            WriteOperatorAction(OperatorJob.HideSubtitles, null, HintResourceSubtitlesHidden);
        }

        public void ShowSubtitles()
        {
            Tick();
            SubtitlesVisible = true;
            WriteOperatorAction(OperatorJob.ShowSubtitles, null, HintResourceSubtitlesShown);
        }

        public void CaptureRegionSelected()
        {
            CaptureRegionSelected(0);
        }

        public void CaptureRegionSelected(int pairId)
        {
            Tick();
            int index = IndexOfPair(pairId);
            int? ordinal = index >= 0 ? index + 1 : (int?)null;
            WriteOperatorAction(OperatorJob.BoxCapture, ordinal, HintResourceCaptureRegionBoxed);
        }

        public void CaptureRegionSelectionCancelled()
        {
            Tick();
            WriteOperatorAction(OperatorJob.BoxCapture, null, HintResourceCaptureRegionMissing);
        }

        public void Refresh(bool hasCaptureRegion, bool foundText)
        {
            Tick();
            if (!hasCaptureRegion)
            {
                WriteOperatorAction(OperatorJob.Refresh, null, HintResourceCaptureRegionMissing);
                return;
            }

            if (foundText)
            {
                WriteOperatorAction(OperatorJob.Refresh, null, HintResourceRefreshed);
            }
            else
            {
                WriteOperatorAction(OperatorJob.Refresh, null, HintResourceRefreshFoundNoText);
            }
        }

        public void ChangeVoiceSpeed(double speed)
        {
            Tick();
            string speedText = speed.ToString("0.##", CultureInfo.InvariantCulture);
            WriteOperatorAction(OperatorJob.VoiceSpeed, null, HintResourceVoiceSpeed, speedText);
        }

        public void NoteOcrMiss()
        {
            Tick();
        }

        public void NoteMatchMiss()
        {
            Tick();
        }

        private void ExpireHintIfNeeded()
        {
            if (HintVisible && _hintExpiresAt.HasValue && _utcNow() >= _hintExpiresAt.Value)
            {
                ClearHint();
            }
        }

        private void WriteOperatorAction(
            OperatorJob job,
            int? pairOrdinal,
            string resultResourceKey,
            params object[] formatArguments)
        {
            DateTime now = _utcNow();
            object[] args = formatArguments == null || formatArguments.Length == 0
                ? null
                : (object[])formatArguments.Clone();
            HintResourceKey = resultResourceKey;
            HintFormatArguments = args;
            HintVisible = true;
            _hintExpiresAt = now.AddMilliseconds(HintDurationMs);
            HintChanged?.Invoke(this, EventArgs.Empty);

            _activityLog.Add(new ActivityLogRow(now, job, pairOrdinal, resultResourceKey, args));
            ActivityLogChanged?.Invoke(this, EventArgs.Empty);
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
}
