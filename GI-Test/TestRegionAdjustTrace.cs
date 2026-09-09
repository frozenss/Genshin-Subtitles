#if DEBUG
using GI_Subtitles.Core.Overlay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GI_Test
{
    /// <summary>
    /// The region-adjust diagnostic summary is Debug-only (issue #37), so these
    /// tests compile and run only in Debug builds of the test project.
    /// </summary>
    [TestClass]
    public class TestRegionAdjustTrace
    {
        [TestMethod]
        public void Summarize_NoInputAtAll_InputNeverReachedWindow()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 0,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 0,
                hitModeRemovalFailed: false,
                frameCenterMismatch: false,
                cursorForeignInsideFrame: false);

            Assert.AreEqual("input-never-reached-window", verdict);
        }

        [TestMethod]
        public void Summarize_WindowInputButNoElementInput_ReachedWindowNotElement()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 7,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 0,
                hitModeRemovalFailed: false,
                frameCenterMismatch: false,
                cursorForeignInsideFrame: false);

            Assert.AreEqual("window-input-reached-but-not-element", verdict);
        }

        [TestMethod]
        public void Summarize_ElementInputWithoutPersist_ReachedElementNotPersisted()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 7,
                elementDownEvents: 2,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 0,
                hitModeRemovalFailed: false,
                frameCenterMismatch: false,
                cursorForeignInsideFrame: false);

            Assert.AreEqual("element-input-reached-but-not-persisted", verdict);
        }

        [TestMethod]
        public void Summarize_PersistHappened_Healthy()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 7,
                elementDownEvents: 1,
                elementMoveEvents: 30,
                elementUpEvents: 1,
                persistEvents: 3,
                hitModeRemovalFailed: false,
                frameCenterMismatch: false,
                cursorForeignInsideFrame: false);

            Assert.AreEqual("element-input-reached-and-persisted", verdict);
        }

        [TestMethod]
        public void Summarize_PersistWithoutElementInput_FallsThroughToLowerVerdict()
        {
            // A settings-page edit while armed persists without any element
            // input; that alone must not read as the healthy drag path.
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 5,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 2,
                hitModeRemovalFailed: false,
                frameCenterMismatch: false,
                cursorForeignInsideFrame: false);

            Assert.AreEqual("window-input-reached-but-not-element", verdict);
        }

        [TestMethod]
        public void Summarize_PersistWithoutAnyInput_InputNeverReachedWindow()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 0,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 1,
                hitModeRemovalFailed: false,
                frameCenterMismatch: false,
                cursorForeignInsideFrame: false);

            Assert.AreEqual("input-never-reached-window", verdict);
        }

        [TestMethod]
        public void Summarize_HitModeFailure_AppendsNote()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 0,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 0,
                hitModeRemovalFailed: true,
                frameCenterMismatch: false,
                cursorForeignInsideFrame: false);

            Assert.AreEqual(
                "input-never-reached-window [hit-mode-transparent-bit-still-set]",
                verdict);
        }

        [TestMethod]
        public void Summarize_FrameCenterMismatch_AppendsNote()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 4,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 0,
                hitModeRemovalFailed: false,
                frameCenterMismatch: true,
                cursorForeignInsideFrame: false);

            Assert.AreEqual(
                "window-input-reached-but-not-element [frame-center-owned-by-other-window]",
                verdict);
        }

        [TestMethod]
        public void Summarize_BothFailures_AppendsBothNotes()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 0,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 0,
                hitModeRemovalFailed: true,
                frameCenterMismatch: true,
                cursorForeignInsideFrame: false);

            Assert.AreEqual(
                "input-never-reached-window "
                    + "[hit-mode-transparent-bit-still-set; frame-center-owned-by-other-window]",
                verdict);
        }

        [TestMethod]
        public void Summarize_CursorForeignInsideInteractiveFrame_AppendsNote()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 0,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 0,
                hitModeRemovalFailed: false,
                frameCenterMismatch: false,
                cursorForeignInsideFrame: true);

            Assert.AreEqual(
                "input-never-reached-window [cursor-inside-frame-owned-by-foreign-window]",
                verdict);
        }

        [TestMethod]
        public void Summarize_AllThreeFailures_AppendsAllNotesInOrder()
        {
            string verdict = RegionAdjustTrace.Summarize(
                windowInputEvents: 0,
                elementDownEvents: 0,
                elementMoveEvents: 0,
                elementUpEvents: 0,
                persistEvents: 0,
                hitModeRemovalFailed: true,
                frameCenterMismatch: true,
                cursorForeignInsideFrame: true);

            Assert.AreEqual(
                "input-never-reached-window [hit-mode-transparent-bit-still-set; "
                    + "frame-center-owned-by-other-window; "
                    + "cursor-inside-frame-owned-by-foreign-window]",
                verdict);
        }
    }
}
#endif
