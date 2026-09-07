using System;
using GI_Subtitles.Core.Overlay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GI_Test
{
    [TestClass]
    public class TestLiveOverlaySessionAppliedGame
    {
        [TestMethod]
        public void ApplyToAnotherGame_ClosesDialogueOptionScanGate_WithoutChangingPairsOrDisplays()
        {
            DateTime now = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            var store = CreateGenshinStore();
            var session = new LiveOverlaySession(new MemoryOcrIntervalStore(), store, () => now);

            Assert.AreEqual("Genshin", session.AppliedGame);
            Assert.IsTrue(session.DialogueOptionScanOn);
            Assert.IsTrue(session.AllowsDialogueOptionScan);

            session.Beat(ExtraPathSample.DialogueOptionsReady(), PairFrameSample.Unchanged());
            Assert.AreEqual(LiveOverlaySession.DialogueOptionsOcrSlot, session.BusyOcrSlot);
            session.CompleteOcr(miss: true);

            session.ApplyGame("StarRail");

            Assert.AreEqual("StarRail", session.AppliedGame);
            Assert.IsTrue(session.DialogueOptionScanOn, "Layout toggle stays; this ticket does not swap overlay layout.");
            Assert.IsFalse(session.AllowsDialogueOptionScan);
            Assert.AreEqual(1, session.Pairs.Count);
            Assert.AreEqual(10, session.Pairs[0].Capture.X);
            Assert.AreEqual(10, session.Pairs[0].Display.X);
            Assert.AreEqual(200, session.DialogueOptionDisplay.X);
            Assert.AreEqual(300, session.DialogueOptionDisplay.Y);

            now = now.AddMilliseconds(400);
            session.Beat(ExtraPathSample.DialogueOptionsReady(), PairFrameSample.Unchanged());
            Assert.IsNull(session.BusyOcrSlot);
            Assert.AreEqual(0, session.OcrQueue.Count);
        }

        [TestMethod]
        public void ApplyBackToGenshin_OpensDialogueOptionScanGate_WhenLayoutToggleIsOn()
        {
            DateTime now = new DateTime(2026, 9, 7, 13, 0, 0, DateTimeKind.Utc);
            var session = new LiveOverlaySession(new MemoryOcrIntervalStore(), CreateGenshinStore(), () => now);

            session.ApplyGame("StarRail");
            Assert.IsFalse(session.AllowsDialogueOptionScan);

            session.ApplyGame("Genshin");

            Assert.AreEqual("Genshin", session.AppliedGame);
            Assert.IsTrue(session.DialogueOptionScanOn);
            Assert.IsTrue(session.AllowsDialogueOptionScan);
            Assert.AreEqual(1, session.Pairs.Count);
            Assert.AreEqual(200, session.DialogueOptionDisplay.X);

            now = now.AddMilliseconds(400);
            session.Beat(ExtraPathSample.DialogueOptionsReady(), PairFrameSample.Unchanged());
            Assert.AreEqual(LiveOverlaySession.DialogueOptionsOcrSlot, session.BusyOcrSlot);
        }

        [TestMethod]
        public void ApplyBackToGenshin_LeavesDialogueOptionScanGateClosed_WhenLayoutToggleIsOff()
        {
            var store = CreateGenshinStore();
            store.DialogueOptionScan = false;
            var session = new LiveOverlaySession(new MemoryOcrIntervalStore(), store);

            session.ApplyGame("StarRail");
            session.ApplyGame("Genshin");

            Assert.IsFalse(session.DialogueOptionScanOn);
            Assert.IsFalse(session.AllowsDialogueOptionScan);
        }

        [TestMethod]
        public void ApplyToAnotherGame_ClosesGenshinLocalVoiceGate_UntilGenshinIsAppliedAgain()
        {
            var session = new LiveOverlaySession(new MemoryOcrIntervalStore(), CreateGenshinStore());

            Assert.IsTrue(session.AllowsGenshinLocalVoice);

            session.ApplyGame("StarRail");
            Assert.IsFalse(session.AllowsGenshinLocalVoice);

            session.ApplyGame("Genshin");
            Assert.IsTrue(session.AllowsGenshinLocalVoice);
        }

        [TestMethod]
        public void ApplyThatChangesGame_ClearsOcrTranslationMatchCache()
        {
            var session = new LiveOverlaySession(new MemoryOcrIntervalStore(), CreateGenshinStore());
            session.RememberMatch("ocr-line", "genshin-translation", "voice-key");

            Assert.IsTrue(session.TryGetCachedMatch("ocr-line", out string cached));
            Assert.AreEqual("genshin-translation", cached);
            Assert.AreEqual("voice-key", session.GetCachedMatchKey(cached));

            session.ApplyGame("StarRail");

            Assert.IsFalse(session.TryGetCachedMatch("ocr-line", out _));
        }

        [TestMethod]
        public void ApplyThatDoesNotChangeGame_LeavesOcrTranslationMatchCache()
        {
            var session = new LiveOverlaySession(new MemoryOcrIntervalStore(), CreateGenshinStore());
            session.RememberMatch("ocr-line", "genshin-translation", "voice-key");

            session.ApplyGame("Genshin");

            Assert.IsTrue(session.TryGetCachedMatch("ocr-line", out string cached));
            Assert.AreEqual("genshin-translation", cached);
            Assert.AreEqual("voice-key", session.GetCachedMatchKey(cached));
        }

        private static MemoryRegionPairStore CreateGenshinStore()
        {
            return new MemoryRegionPairStore
            {
                StoredPairs =
                {
                    new RegionPairRecord
                    {
                        Id = 1,
                        Capture = new OverlayRect(10, 20, 80, 20),
                        Display = new OverlayRect(10, 50, 80, 20)
                    }
                },
                VoicePrimaryId = 1,
                NextPairId = 2,
                DialogueOptionScan = true,
                DialogueOptionDisplay = new OverlayRect(200, 300, 160, 40)
            };
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
            public OverlayRect DarkScreenDisplay = OverlayRect.Invalid;
            public OverlayRect DialogueOptionDisplay = OverlayRect.Invalid;
            public bool DarkScreenScan = true;
            public bool DialogueOptionScan;

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

            public OverlayRect ReadDarkScreenDisplay()
            {
                return DarkScreenDisplay ?? OverlayRect.Invalid;
            }

            public void WriteDarkScreenDisplay(OverlayRect display)
            {
                DarkScreenDisplay = display ?? OverlayRect.Invalid;
            }

            public OverlayRect ReadDialogueOptionDisplay()
            {
                return DialogueOptionDisplay ?? OverlayRect.Invalid;
            }

            public void WriteDialogueOptionDisplay(OverlayRect display)
            {
                DialogueOptionDisplay = display ?? OverlayRect.Invalid;
            }

            public bool ReadDarkScreenScan()
            {
                return DarkScreenScan;
            }

            public void WriteDarkScreenScan(bool enabled)
            {
                DarkScreenScan = enabled;
            }

            public bool ReadDialogueOptionScan()
            {
                return DialogueOptionScan;
            }

            public void WriteDialogueOptionScan(bool enabled)
            {
                DialogueOptionScan = enabled;
            }
        }
    }
}
