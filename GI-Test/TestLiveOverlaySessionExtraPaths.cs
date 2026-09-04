using System;
using GI_Subtitles.Core.Overlay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GI_Test
{
    [TestClass]
    public class TestLiveOverlaySessionExtraPaths
    {
        [TestMethod]
        public void ExtraPathOnABeat_DoesNotSkipPairMissKeep()
        {
            DateTime now = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc);
            LiveOverlaySession session = CreateSessionWithPairs(1, () => now);
            OverlayRect band = new OverlayRect(40, 80, 400, 60);

            session.Beat(PairFrameSample.ChangedAndStable());
            session.CompleteOcr(miss: false, content: "keep-me");

            now = now.AddMilliseconds(400);
            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true),
                PairFrameSample.ChangedAndStable());
            session.CompleteOcr(miss: false, content: "cutscene");
            now = now.AddMilliseconds(400);
            session.Tick();
            session.CompleteOcr(miss: true);

            Assert.AreEqual("keep-me", session.PairBodies[0].Content);
            Assert.AreEqual("cutscene", session.DarkScreenBody.Content);
        }

        [TestMethod]
        public void ExtraPathOnABeat_DoesNotSkipPairDiffsOrClears()
        {
            DateTime now = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
            LiveOverlaySession session = CreateSessionWithPairs(2, () => now);
            OverlayRect band = new OverlayRect(40, 80, 400, 60);

            session.Beat(PairFrameSample.ChangedAndStable(), PairFrameSample.ChangedAndStable());
            session.CompleteOcr(miss: false, content: "pair-one");
            now = now.AddMilliseconds(400);
            session.Tick();
            session.CompleteOcr(miss: false, content: "pair-two");

            now = now.AddMilliseconds(400);
            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true)
                    .WithDialogueOptionsReady(),
                PairFrameSample.StableNoText(),
                PairFrameSample.ChangedAndStable());

            Assert.AreEqual(string.Empty, session.PairBodies[0].Content);
            Assert.AreEqual("pair-two", session.PairBodies[1].Content);
            Assert.AreEqual(LiveOverlaySession.DarkScreenOcrSlot, session.BusyOcrSlot);
            CollectionAssert.AreEqual(
                new[] { LiveOverlaySession.DialogueOptionsOcrSlot, 1 },
                System.Linq.Enumerable.ToArray(session.OcrQueue));
        }

        [TestMethod]
        public void ContendedBeat_EnqueuesDarkScreenThenDialogueOptionsThenPairs()
        {
            DateTime now = new DateTime(2026, 6, 1, 13, 0, 0, DateTimeKind.Utc);
            LiveOverlaySession session = CreateSessionWithPairs(2, () => now);
            OverlayRect band = new OverlayRect(10, 20, 300, 40);

            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true)
                    .WithDialogueOptionsReady(),
                PairFrameSample.ChangedAndStable(),
                PairFrameSample.ChangedAndStable());

            Assert.AreEqual(LiveOverlaySession.DarkScreenOcrSlot, session.BusyOcrSlot);
            Assert.IsNull(session.BusyOcrPairIndex);
            CollectionAssert.AreEqual(
                new[] { LiveOverlaySession.DialogueOptionsOcrSlot, 0, 1 },
                System.Linq.Enumerable.ToArray(session.OcrQueue));

            session.CompleteOcr(miss: false, content: "cutscene", header: "narrator");
            Assert.AreEqual("cutscene", session.DarkScreenBody.Content);
            Assert.AreEqual("narrator", session.DarkScreenBody.Header);
            Assert.AreEqual(10, session.DarkScreenBody.Display.X);
            Assert.IsTrue(session.DarkScreenBody.Visible);
            Assert.IsNull(session.BusyOcrSlot);
            CollectionAssert.AreEqual(
                new[] { LiveOverlaySession.DialogueOptionsOcrSlot, 0, 1 },
                System.Linq.Enumerable.ToArray(session.OcrQueue));

            now = now.AddMilliseconds(400);
            session.Tick();
            Assert.AreEqual(LiveOverlaySession.DialogueOptionsOcrSlot, session.BusyOcrSlot);

            session.CompleteOcr(miss: false, content: "option-list");
            Assert.AreEqual("cutscene", session.DarkScreenBody.Content);
            Assert.AreEqual(string.Empty, session.PairBodies[0].Content, "Dialogue-option OCR is not in-place overlay text.");

            now = now.AddMilliseconds(400);
            session.Tick();
            Assert.AreEqual(0, session.BusyOcrPairIndex);
            session.CompleteOcr(miss: false, content: "pair-one");
            Assert.AreEqual("pair-one", session.PairBodies[0].Content);
            Assert.IsTrue(session.PairBodies[0].RecognitionOrder > session.DarkScreenBody.RecognitionOrder);
        }

        [TestMethod]
        public void DialogueChoiceEcho_SitsOnVoicePrimary_AndIsOmittedWhenNone()
        {
            DateTime now = new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);
            LiveOverlaySession session = CreateSessionWithPairs(2, () => now);
            session.SetVoicePrimary(2);

            session.Beat(ExtraPathSample.DialogueChoice("跳过"));

            ExtraPathBody echo = session.DialogueChoiceEcho;
            Assert.IsTrue(echo.Visible);
            Assert.AreEqual("◆ 跳过", echo.Content);
            Assert.AreEqual(session.Pairs[1].Display.X, echo.Display.X);
            Assert.AreEqual(string.Empty, session.PairBodies[0].Content, "Echo must not replace the pair body.");
            Assert.AreEqual(string.Empty, session.PairBodies[1].Content, "Echo must not replace the pair body.");
            Assert.IsFalse(session.HintVisible);

            now = now.AddSeconds(3);
            session.Tick();
            Assert.IsFalse(session.DialogueChoiceEcho.Visible);
            Assert.AreEqual(string.Empty, session.DialogueChoiceEcho.Content);

            session.DeletePair(1);
            session.DeletePair(2);
            Assert.AreEqual(0, session.VoicePrimaryId);

            session.Beat(ExtraPathSample.DialogueChoice("再跳过"));
            Assert.IsFalse(session.DialogueChoiceEcho.Visible);
            Assert.AreEqual(string.Empty, session.DialogueChoiceEcho.Content);
        }

        [TestMethod]
        public void ExtraPathVoice_DoesNotRebindOrInterrupt()
        {
            DateTime now = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
            LiveOverlaySession session = CreateSessionWithPairs(2, () => now);
            OverlayRect band = new OverlayRect(8, 9, 200, 30);

            session.Beat(PairFrameSample.ChangedAndStable(), PairFrameSample.Unchanged());
            session.CompleteOcr(miss: false, content: "primary-line");
            VoicePlayRequest primary = session.TakeVoicePlayRequest();
            Assert.IsNotNull(primary);
            Assert.IsFalse(primary.ExtraPath);
            Assert.AreEqual(1, primary.PairId);
            Assert.AreEqual(1, session.VoicePrimaryId);
            int token = session.VoicePlaybackToken;

            now = now.AddMilliseconds(400);
            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true),
                PairFrameSample.Unchanged(),
                PairFrameSample.Unchanged());
            session.CompleteOcr(miss: false, content: "cutscene");
            Assert.IsNull(session.TakeVoicePlayRequest());
            Assert.AreEqual(1, session.VoicePrimaryId);
            Assert.AreEqual(token, session.VoicePlaybackToken);
            Assert.IsTrue(session.VoicePlaybackActive);

            session.NoteVoicePlaybackEnded();
            now = now.AddMilliseconds(400);
            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true),
                PairFrameSample.Unchanged(),
                PairFrameSample.Unchanged());
            session.CompleteOcr(miss: false, content: "cutscene");
            VoicePlayRequest darkVoice = session.TakeVoicePlayRequest();
            Assert.IsNotNull(darkVoice);
            Assert.IsTrue(darkVoice.ExtraPath);
            Assert.AreEqual(1, session.VoicePrimaryId);
            Assert.AreEqual("cutscene", darkVoice.Content);

            session.NoteVoicePlaybackEnded();
            now = now.AddMilliseconds(400);
            session.Beat(ExtraPathSample.DialogueChoice("同意"));
            VoicePlayRequest echoVoice = session.TakeVoicePlayRequest();
            Assert.IsNotNull(echoVoice);
            Assert.IsTrue(echoVoice.ExtraPath);
            Assert.AreEqual(1, session.VoicePrimaryId);
            Assert.AreEqual("◆ 同意", echoVoice.Content);
            Assert.IsTrue(session.VoicePlaybackActive);

            now = now.AddMilliseconds(400);
            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true),
                PairFrameSample.Unchanged(),
                PairFrameSample.Unchanged());
            session.CompleteOcr(miss: false, content: "later-card");
            Assert.IsNull(session.TakeVoicePlayRequest());
            Assert.AreEqual(1, session.VoicePrimaryId);
        }

        [TestMethod]
        public void DarkScreenEnd_ClearsOnlyThatLine()
        {
            DateTime now = new DateTime(2026, 6, 4, 12, 0, 0, DateTimeKind.Utc);
            LiveOverlaySession session = CreateSessionWithPairs(2, () => now);
            OverlayRect band = new OverlayRect(12, 24, 180, 36);

            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true),
                PairFrameSample.ChangedAndStable(),
                PairFrameSample.Unchanged());
            session.CompleteOcr(miss: false, content: "card");
            now = now.AddMilliseconds(400);
            session.Tick();
            session.CompleteOcr(miss: false, content: "speaker");
            session.Beat(ExtraPathSample.DialogueChoice("走开"));

            Assert.AreEqual("card", session.DarkScreenBody.Content);
            Assert.AreEqual("speaker", session.PairBodies[0].Content);
            Assert.AreEqual("◆ 走开", session.DialogueChoiceEcho.Content);

            session.Beat(
                ExtraPathSample.DarkScreenEnded(),
                PairFrameSample.Unchanged(),
                PairFrameSample.Unchanged());

            Assert.AreEqual(string.Empty, session.DarkScreenBody.Content);
            Assert.IsFalse(session.DarkScreenBody.Visible);
            Assert.AreEqual("speaker", session.PairBodies[0].Content);
            Assert.AreEqual("◆ 走开", session.DialogueChoiceEcho.Content);

            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true),
                PairFrameSample.Unchanged(),
                PairFrameSample.Unchanged());
            session.CompleteOcr(miss: false, content: "card-again");
            session.Beat(
                ExtraPathSample.DarkScreenWithoutCandidate(),
                PairFrameSample.Unchanged(),
                PairFrameSample.Unchanged());
            Assert.AreEqual(string.Empty, session.DarkScreenBody.Content);
            Assert.AreEqual("speaker", session.PairBodies[0].Content);
        }

        [TestMethod]
        public void ExtraPathTexts_HideWithSubtitles_AndAreNotHints()
        {
            LiveOverlaySession session = CreateSessionWithPairs(1);
            OverlayRect band = new OverlayRect(1, 2, 100, 20);

            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true),
                PairFrameSample.ChangedAndStable());
            session.CompleteOcr(miss: false, content: "card");
            session.Beat(ExtraPathSample.DialogueChoice("好的"));

            Assert.IsFalse(session.HintVisible);
            Assert.IsNull(session.HintResourceKey);
            Assert.IsTrue(session.DarkScreenBody.Visible);
            Assert.IsTrue(session.DialogueChoiceEcho.Visible);

            session.HideSubtitles();

            Assert.IsFalse(session.DarkScreenBody.Visible);
            Assert.IsFalse(session.DialogueChoiceEcho.Visible);
            Assert.AreEqual("card", session.DarkScreenBody.Content);
            Assert.AreEqual("◆ 好的", session.DialogueChoiceEcho.Content);
            Assert.IsTrue(session.HintVisible);
            Assert.AreEqual("Hint_SubtitlesHidden", session.HintResourceKey);

            session.ShowSubtitles();
            Assert.IsTrue(session.DarkScreenBody.Visible);
            Assert.IsTrue(session.DialogueChoiceEcho.Visible);
        }

        [TestMethod]
        public void NoValidCapture_DoesNotRunExtraPaths()
        {
            var pairs = new MemoryRegionPairStore();
            var session = new LiveOverlaySession(new MemoryOcrIntervalStore(), pairs);
            OverlayRect band = new OverlayRect(10, 20, 30, 40);

            session.Beat(
                ExtraPathSample.DarkScreenCandidate(band, needsOcr: true)
                    .WithDialogueOptionsReady()
                    .WithDialogueChoice("无主区"));

            Assert.IsNull(session.BusyOcrSlot);
            Assert.AreEqual(0, session.OcrQueue.Count);
            Assert.IsFalse(session.DarkScreenBody.Visible);
            Assert.AreEqual(string.Empty, session.DarkScreenBody.Content);
            Assert.IsFalse(session.DialogueChoiceEcho.Visible);
            Assert.IsNull(session.TakeVoicePlayRequest());
        }

        private static LiveOverlaySession CreateSessionWithPairs(int pairCount, Func<DateTime> utcNow = null)
        {
            var records = new System.Collections.Generic.List<RegionPairRecord>();
            for (int i = 0; i < pairCount; i++)
            {
                records.Add(new RegionPairRecord
                {
                    Capture = new OverlayRect(i * 100, 10, 80, 20),
                    Display = new OverlayRect(i * 100, 40, 80, 20)
                });
            }

            return new LiveOverlaySession(
                new MemoryOcrIntervalStore(),
                new MemoryRegionPairStore { StoredPairs = records },
                utcNow);
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

        private sealed class MemoryRegionPairStore : IRegionPairStore
        {
            public LegacyRegionSlots Legacy = new LegacyRegionSlots();
            public System.Collections.Generic.List<RegionPairRecord> StoredPairs =
                new System.Collections.Generic.List<RegionPairRecord>();
            public int VoicePrimaryId;
            public int NextPairId;
            public int WriteCount;

            public System.Collections.Generic.IReadOnlyList<RegionPairRecord> ReadPairs()
            {
                return StoredPairs;
            }

            public LegacyRegionSlots ReadLegacy()
            {
                return Legacy;
            }

            public void WritePairs(System.Collections.Generic.IReadOnlyList<RegionPairRecord> pairs)
            {
                StoredPairs = new System.Collections.Generic.List<RegionPairRecord>(pairs);
                WriteCount++;
            }

            public int ReadVoicePrimaryId()
            {
                return VoicePrimaryId;
            }

            public void WriteVoicePrimaryId(int id)
            {
                VoicePrimaryId = id;
            }

            public int ReadNextPairId()
            {
                return NextPairId;
            }

            public void WriteNextPairId(int id)
            {
                NextPairId = id;
            }
        }
    }
}
