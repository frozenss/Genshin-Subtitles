using System;
using GI_Subtitles.Core.Overlay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GI_Test
{
    [TestClass]
    public class TestLiveOverlaySessionHint
    {
        [TestMethod]
        public void StartRecognition_WithCaptureRegion_ShowsRecognizingHint()
        {
            LiveOverlaySession session = CreateSession();

            session.StartRecognition(hasCaptureRegion: true);

            Assert.IsTrue(session.HintVisible);
            Assert.AreEqual("识别中", session.HintText);
        }

        [TestMethod]
        public void StartRecognition_FromHotkeyAndSettings_SharesTheSameHint()
        {
            LiveOverlaySession session = CreateSession();

            session.StartRecognition(hasCaptureRegion: true);
            Assert.AreEqual("识别中", session.HintText);

            session.StopRecognition();
            Assert.AreEqual("已停止", session.HintText);

            session.StartRecognition(hasCaptureRegion: true);
            Assert.AreEqual("识别中", session.HintText);
            Assert.IsTrue(session.RecognitionRunning);
        }

        [TestMethod]
        public void NewHint_ReplacesTheOldAndRestartsTheTwoSecondClock()
        {
            DateTime now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            LiveOverlaySession session = CreateSession(() => now);

            session.StartRecognition(hasCaptureRegion: true);
            Assert.AreEqual("识别中", session.HintText);

            now = now.AddSeconds(1);
            session.HideSubtitles();
            Assert.AreEqual("字幕已隐藏", session.HintText);
            Assert.IsTrue(session.HintVisible);

            now = now.AddSeconds(1);
            session.Tick();
            Assert.IsTrue(session.HintVisible, "Clock restarted at replace, so 1s later the hint is still live.");

            now = now.AddSeconds(1);
            session.Tick();
            Assert.IsFalse(session.HintVisible);
            Assert.IsNull(session.HintText);
        }

        [TestMethod]
        public void HideSubtitles_LeavesALiveHintVisible()
        {
            LiveOverlaySession session = CreateSession();

            session.StartRecognition(hasCaptureRegion: true);
            session.HideSubtitles();

            Assert.IsFalse(session.SubtitlesVisible);
            Assert.IsTrue(session.HintVisible);
            Assert.AreEqual("字幕已隐藏", session.HintText);

            session.ShowSubtitles();
            Assert.IsTrue(session.SubtitlesVisible);
            Assert.IsTrue(session.HintVisible);
            Assert.AreEqual("字幕已显示", session.HintText);
        }

        [TestMethod]
        public void SuccessfulPreview_ProducesNoHint()
        {
            LiveOverlaySession session = CreateSession();

            session.PreviewCaptureRegion(hasCaptureRegion: true);

            Assert.IsFalse(session.HintVisible);
            Assert.IsNull(session.HintText);
        }

        [TestMethod]
        public void SuccessfulPreview_DoesNotReplaceALiveHint()
        {
            LiveOverlaySession session = CreateSession();

            session.StartRecognition(hasCaptureRegion: true);
            session.PreviewCaptureRegion(hasCaptureRegion: true);

            Assert.AreEqual("识别中", session.HintText);
            Assert.IsTrue(session.HintVisible);
        }

        [TestMethod]
        public void ActionFailures_HintTheActionJustTaken()
        {
            LiveOverlaySession session = CreateSession();

            session.StartRecognition(hasCaptureRegion: false);
            Assert.AreEqual("未设置识别区", session.HintText);
            Assert.IsFalse(session.RecognitionRunning);

            session.CaptureRegionSelectionCancelled();
            Assert.AreEqual("未设置识别区", session.HintText);

            session.Refresh(hasCaptureRegion: true, foundText: false);
            Assert.AreEqual("未识别到文本", session.HintText);

            session.Refresh(hasCaptureRegion: false, foundText: false);
            Assert.AreEqual("未设置识别区", session.HintText);

            session.PreviewCaptureRegion(hasCaptureRegion: false);
            Assert.AreEqual("未设置识别区", session.HintText);
        }

        [TestMethod]
        public void OcrAndMatchMisses_DoNotHint()
        {
            LiveOverlaySession session = CreateSession();

            session.NoteOcrMiss();
            session.NoteMatchMiss();

            Assert.IsFalse(session.HintVisible);
            Assert.IsNull(session.HintText);

            session.StartRecognition(hasCaptureRegion: true);
            session.NoteOcrMiss();
            session.NoteMatchMiss();

            Assert.AreEqual("识别中", session.HintText);
            Assert.IsTrue(session.HintVisible);
        }

        [TestMethod]
        public void OperatorActions_ShowLockedResultCopy()
        {
            LiveOverlaySession session = CreateSession();

            session.CaptureRegionSelected();
            Assert.AreEqual("已选识别区", session.HintText);

            session.Refresh(hasCaptureRegion: true, foundText: true);
            Assert.AreEqual("已刷新", session.HintText);

            session.ChangeVoiceSpeed(1.5);
            Assert.AreEqual("倍速 1.5×", session.HintText);

            session.ChangeVoiceSpeed(1.25);
            Assert.AreEqual("倍速 1.25×", session.HintText);

            session.ChangeVoiceSpeed(2.0);
            Assert.AreEqual("倍速 2×", session.HintText);
        }

        private static LiveOverlaySession CreateSession(Func<DateTime> utcNow = null)
        {
            return new LiveOverlaySession(new MemoryOcrIntervalStore(), utcNow);
        }

        private sealed class MemoryOcrIntervalStore : IOcrIntervalStore
        {
            public int Read(int defaultValue)
            {
                return defaultValue;
            }

            public void Write(int milliseconds)
            {
            }
        }
    }
}
