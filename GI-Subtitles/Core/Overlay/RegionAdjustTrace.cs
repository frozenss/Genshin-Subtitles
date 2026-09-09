using System.Diagnostics;
using GI_Subtitles.Common;

namespace GI_Subtitles.Core.Overlay
{
    /// <summary>
    /// Debug-only diagnostic trace for the region-adjust pipeline (issue #37):
    /// arm entry, outline rebuild, mouse events, and persistence. Every public
    /// method is [Conditional("DEBUG")] and every body is compiled out unless
    /// DEBUG is defined, so Release artifacts carry none of this code.
    ///
    /// The trace also accumulates evidence while armed and prints a verdict on
    /// disarm that names which link of the chain died: input never reached the
    /// window, input reached the window but no frame element, input reached an
    /// element but nothing persisted, or the healthy path.
    /// </summary>
    public static class RegionAdjustTrace
    {
        [Conditional("DEBUG")]
        public static void ArmEntryRefused(OverlayAdjustTarget target, int pairId, string detail)
        {
#if DEBUG
            Write("arm refused: target=" + target + " pairId=" + pairId + " detail=" + detail);
#endif
        }

        [Conditional("DEBUG")]
        public static void ArmAccepted(OverlayAdjustTarget target, int pairId, int outlineCount)
        {
#if DEBUG
            ResetEvidence();
            Write("arm accepted: target=" + target + " pairId=" + pairId
                + " outlines=" + outlineCount);
#endif
        }

        [Conditional("DEBUG")]
        public static void OutlineBuilt(RegionOutline outline)
        {
#if DEBUG
            if (outline == null || outline.Rect == null)
            {
                Write("outline: <null>");
                return;
            }

            Write("outline: kind=" + outline.Kind + " pairOrdinal=" + outline.PairOrdinal
                + " rect=" + outline.Rect.ToCsv() + " valid=" + outline.Rect.IsValid
                + " isDisplay=" + outline.IsDisplay);
#endif
        }

        [Conditional("DEBUG")]
        public static void ArmDismissed(OverlayAdjustTarget target, int pairId)
        {
#if DEBUG
            Write("arm dismissed: target=" + target + " pairId=" + pairId);
            EmitSummary();
#endif
        }

        [Conditional("DEBUG")]
        public static void DisplaySet(OverlayAdjustTarget target, int pairId, OverlayRect rect)
        {
#if DEBUG
            _persistEvents++;
            if (_persistLogged < MaxRepeatLogs)
            {
                _persistLogged++;
                Write("display set: target=" + target + " pairId=" + pairId
                    + " rect=" + (rect == null ? "<null>" : rect.ToCsv())
                    + " valid=" + (rect != null && rect.IsValid));
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void StoreWrite(string what, string detail)
        {
#if DEBUG
            if (_storeWriteLogged < MaxRepeatLogs)
            {
                _storeWriteLogged++;
                Write("store write: " + what + " " + detail);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void ElementDown(
            AdjustMouseExit exit,
            OverlayAdjustTarget target,
            int pairIndex,
            OverlayRect startRect)
        {
#if DEBUG
            _elementDownEvents++;
            Write("element mouse-down: " + Describe(exit) + " target=" + target
                + " pairIndex=" + pairIndex
                + " start=" + (startRect == null ? "<null>" : startRect.ToCsv()));
#endif
        }

        [Conditional("DEBUG")]
        public static void ElementMove(AdjustMouseExit exit, double x, double y)
        {
#if DEBUG
            _elementMoveEvents++;
            if (_elementMoveLogged < MaxRepeatLogs)
            {
                _elementMoveLogged++;
                Write("element mouse-move: " + Describe(exit)
                    + " pos=" + FormatPoint(x, y));
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void ElementUp(AdjustMouseExit exit)
        {
#if DEBUG
            _elementUpEvents++;
            Write("element mouse-up: " + Describe(exit));
#endif
        }

        [Conditional("DEBUG")]
        public static void NoteWindowInput(string kind, double x, double y)
        {
#if DEBUG
            _windowInputEvents++;
            if (_windowInputLogged < MaxRepeatLogs)
            {
                _windowInputLogged++;
                Write("window input: " + kind + " pos=" + FormatPoint(x, y)
                    + " (window-level preview; element not yet involved)");
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void NoteHitModeResult(bool interactiveExpected, bool transparentBitCleared)
        {
#if DEBUG
            if (interactiveExpected && !transparentBitCleared)
            {
                _hitModeRemovalFailed = true;
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void NoteFrameProbePostRender(bool takesMouse, bool centerIsOurWindow)
        {
#if DEBUG
            if (takesMouse && !centerIsOurWindow)
            {
                _frameCenterMismatch = true;
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void NoteCursorProbe(bool insideInteractiveFrame, bool overOurWindow)
        {
#if DEBUG
            _cursorProbeTicks++;
            if (insideInteractiveFrame && !overOurWindow)
            {
                _cursorForeignInsideFrame = true;
            }
#endif
        }

        /// <summary>
        /// Turns the collected evidence into the verdict that names the dead
        /// link of the adjust chain. Pure, so tests pin the vocabulary.
        /// </summary>
#if DEBUG
        internal static string Summarize(
            int windowInputEvents,
            int elementDownEvents,
            int elementMoveEvents,
            int elementUpEvents,
            int persistEvents,
            bool hitModeRemovalFailed,
            bool frameCenterMismatch,
            bool cursorForeignInsideFrame)
        {
            string verdict;
            if (persistEvents > 0 && elementDownEvents > 0)
            {
                verdict = "element-input-reached-and-persisted";
            }
            else if (elementDownEvents > 0)
            {
                verdict = "element-input-reached-but-not-persisted";
            }
            else if (windowInputEvents > 0)
            {
                verdict = "window-input-reached-but-not-element";
            }
            else
            {
                verdict = "input-never-reached-window";
            }

            var notes = new System.Collections.Generic.List<string>();
            if (hitModeRemovalFailed)
            {
                notes.Add("hit-mode-transparent-bit-still-set");
            }
            if (frameCenterMismatch)
            {
                notes.Add("frame-center-owned-by-other-window");
            }
            if (cursorForeignInsideFrame)
            {
                notes.Add("cursor-inside-frame-owned-by-foreign-window");
            }

            if (notes.Count == 0)
            {
                return verdict;
            }

            return verdict + " [" + string.Join("; ", notes) + "]";
        }
#endif

#if DEBUG
        private const int MaxRepeatLogs = 3;

        private static int _windowInputEvents;
        private static int _windowInputLogged;
        private static int _elementDownEvents;
        private static int _elementMoveEvents;
        private static int _elementMoveLogged;
        private static int _elementUpEvents;
        private static int _persistEvents;
        private static int _persistLogged;
        private static int _storeWriteLogged;
        private static bool _hitModeRemovalFailed;
        private static bool _frameCenterMismatch;
        private static bool _cursorForeignInsideFrame;
        private static int _cursorProbeTicks;

        private static void ResetEvidence()
        {
            _windowInputEvents = 0;
            _windowInputLogged = 0;
            _elementDownEvents = 0;
            _elementMoveEvents = 0;
            _elementMoveLogged = 0;
            _elementUpEvents = 0;
            _persistEvents = 0;
            _persistLogged = 0;
            _storeWriteLogged = 0;
            _hitModeRemovalFailed = false;
            _frameCenterMismatch = false;
            _cursorForeignInsideFrame = false;
            _cursorProbeTicks = 0;
        }

        private static void EmitSummary()
        {
            Write("summary: window-input=" + _windowInputEvents
                + " element-down=" + _elementDownEvents
                + " element-move=" + _elementMoveEvents
                + " element-up=" + _elementUpEvents
                + " persists=" + _persistEvents
                + " cursor-ticks=" + _cursorProbeTicks
                + " verdict=" + Summarize(
                    _windowInputEvents,
                    _elementDownEvents,
                    _elementMoveEvents,
                    _elementUpEvents,
                    _persistEvents,
                    _hitModeRemovalFailed,
                    _frameCenterMismatch,
                    _cursorForeignInsideFrame));
        }

        private static string Describe(AdjustMouseExit exit)
        {
            switch (exit)
            {
                case AdjustMouseExit.None:
                    return "proceed";
                case AdjustMouseExit.ClickThrough:
                    return "exit=click-through (session reports click-through)";
                case AdjustMouseExit.ArmedPairIndexMissing:
                    return "exit=armed-pair-index-missing";
                case AdjustMouseExit.StartRectInvalid:
                    return "exit=start-rect-invalid";
                case AdjustMouseExit.SenderNotBox:
                    return "exit=sender-not-box";
                case AdjustMouseExit.NotDragging:
                    return "exit=not-dragging";
                case AdjustMouseExit.ButtonReleased:
                    return "exit=button-released";
                case AdjustMouseExit.NoDragTarget:
                    return "exit=no-drag-target";
                default:
                    return "exit=unknown(" + exit + ")";
            }
        }

        private static string FormatPoint(double x, double y)
        {
            return "(" + x.ToString("0.##") + "," + y.ToString("0.##") + ")";
        }

        private static void Write(string message)
        {
            Logger.Log.Debug("[AdjustTrace] " + message);
        }
#endif
    }
}
