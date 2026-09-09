using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using GI_Subtitles.Core.Overlay;

namespace GI_Subtitles.Views
{
    /// <summary>
    /// Debug-only Win32 diagnostics for the region-adjust pipeline (issue #37):
    /// the exStyle switch around WS_EX_TRANSPARENT, a DPI/geometry snapshot,
    /// and a WindowFromPoint probe at each adjust frame's center. The center
    /// probe runs once at arm time (pre-render) and again ~300ms after each
    /// apply pass (post-render, authoritative: the arm-time pass races the
    /// first composition). While armed, a 500ms cursor probe reports who owns
    /// the pixel under the cursor even when no mouse events arrive, plus the
    /// current mouse-capture state. A wndproc hook additionally counts the raw
    /// WM_* mouse messages that reach the hwnd, separating "the OS never
    /// delivered input to our window" (e.g. mouse capture held elsewhere)
    /// from "input reached the hwnd but WPF raised no events". A thread-wide
    /// WH_MOUSE observation hook then watches the same messages one step
    /// earlier, before dispatch, tallying them per target window and naming
    /// each distinct target: messages addressed to the overlay that the
    /// wndproc never receives were eaten between the hook and the wndproc,
    /// and a ComponentDispatcher filter-stage probe splits that span into
    /// "a WPF IMessageFilter consumed it" versus "it vanished before
    /// retrieval (a pre-existing native hook)". Every
    /// public method is [Conditional("DEBUG")] and every body — including the
    /// Win32 declarations — is compiled out unless DEBUG is defined, so Release
    /// artifacts contain none of this code.
    /// </summary>
    public static class RegionAdjustDiagnostics
    {
        [Conditional("DEBUG")]
        public static void HitModeApplied(
            IntPtr hwnd,
            bool interactive,
            int beforeExStyle,
            int newExStyle,
            int setResult,
            int lastError)
        {
#if DEBUG
            if (hwnd == IntPtr.Zero)
            {
                Write("hit-mode: skipped, hwnd is zero");
                return;
            }

            int afterExStyle = GetWindowLong(hwnd, GwlExStyle);
            bool transparentBefore = HasFlag(beforeExStyle);
            bool transparentAfter = HasFlag(afterExStyle);
            bool setFailed = setResult == 0 && lastError != 0;

            string verdict;
            if (setFailed)
            {
                verdict = "SetWindowLong FAILED";
            }
            else if (interactive && transparentAfter)
            {
                verdict = "TRANSPARENT-BIT-STILL-SET";
            }
            else if (!interactive && !transparentAfter)
            {
                verdict = "TRANSPARENT-BIT-NOT-RESTORED";
            }
            else if (afterExStyle != newExStyle)
            {
                verdict = "STYLE-MISMATCH-AFTER-SET";
            }
            else
            {
                verdict = "ok";
            }

            Write("hit-mode interactive=" + interactive
                + " exStyleBefore=0x" + beforeExStyle.ToString("X")
                + " exStyleAfter=0x" + afterExStyle.ToString("X")
                + " requested=0x" + newExStyle.ToString("X")
                + " setReturn=0x" + setResult.ToString("X")
                + " lastError=" + lastError
                + " transparentBefore=" + transparentBefore
                + " transparentAfter=" + transparentAfter
                + " verdict=" + verdict);

            RegionAdjustTrace.NoteHitModeResult(
                interactiveExpected: interactive,
                transparentBitCleared: !transparentAfter);
#endif
        }

        [Conditional("DEBUG")]
        public static void DisabledBitCleared(
            IntPtr hwnd,
            int beforeStyle,
            int newStyle,
            int setResult,
            int lastError)
        {
#if DEBUG
            if (hwnd == IntPtr.Zero)
            {
                Write("disabled-bit: skipped, hwnd is zero");
                return;
            }

            int afterStyle = GetWindowLong(hwnd, GwlStyle);
            bool setFailed = setResult == 0 && lastError != 0;

            string verdict;
            if (setFailed)
            {
                verdict = "SetWindowLong FAILED";
            }
            else if ((afterStyle & WsDisabled) != 0)
            {
                verdict = "DISABLED-BIT-STILL-SET";
            }
            else if (afterStyle != newStyle)
            {
                verdict = "STYLE-MISMATCH-AFTER-SET";
            }
            else
            {
                verdict = "ok";
            }

            Write("disabled-bit cleared: styleBefore=0x" + beforeStyle.ToString("X")
                + " styleAfter=0x" + afterStyle.ToString("X")
                + " requested=0x" + newStyle.ToString("X")
                + " setReturn=0x" + setResult.ToString("X")
                + " lastError=" + lastError
                + " verdict=" + verdict);
#endif
        }

        [Conditional("DEBUG")]
        public static void SnapshotEnvironment(Window window, IntPtr hwnd, double scale)
        {
#if DEBUG
            // Each apply pass rebuilds the outline set: re-register frames and
            // give the fresh composition its own post-render probe.
            _armedFrames.Clear();
            RestartRenderProbe();

            Write("environment: scale=" + scale.ToString("0.###")
                + " systemDpi=" + GetDpiForSystem()
                + " virtualScreenDip=(" + SystemParameters.VirtualScreenLeft.ToString("0.##")
                + "," + SystemParameters.VirtualScreenTop.ToString("0.##")
                + " " + SystemParameters.VirtualScreenWidth.ToString("0.##")
                + "x" + SystemParameters.VirtualScreenHeight.ToString("0.##") + ")");

            if (window != null)
            {
                Write("environment: windowWpfDip=(left=" + window.Left.ToString("0.##")
                    + ",top=" + window.Top.ToString("0.##")
                    + " " + window.Width.ToString("0.##") + "x" + window.Height.ToString("0.##") + ")");
            }

            if (hwnd != IntPtr.Zero)
            {
                Win32Rect rect;
                if (GetWindowRect(hwnd, out rect))
                {
                    Write("environment: windowRectPhysical=(" + rect.Left + "," + rect.Top
                        + " " + (rect.Right - rect.Left) + "x" + (rect.Bottom - rect.Top) + ")");
                }
                else
                {
                    Write("environment: GetWindowRect failed, lastError=" + Marshal.GetLastWin32Error());
                }

                int exStyle = GetWindowLong(hwnd, GwlExStyle);
                Write("environment: exStyle=0x" + exStyle.ToString("X")
                    + " transparent=" + HasFlag(exStyle));
            }

            foreach (string line in DescribeMonitors())
            {
                Write(line);
            }
#endif
        }

        [Conditional("DEBUG")]
        public static void ProbeFrame(
            IntPtr hwnd,
            string label,
            OverlayRect rect,
            Point canvasPoint,
            double boxWidth,
            double boxHeight,
            bool takesMouse)
        {
#if DEBUG
            if (rect == null)
            {
                return;
            }

            var frame = new ArmedFrame
            {
                Label = label,
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height,
                TakesMouse = takesMouse,
                CanvasX = canvasPoint.X,
                CanvasY = canvasPoint.Y,
                BoxWidth = boxWidth,
                BoxHeight = boxHeight
            };
            _armedFrames.Add(frame);
            ProbeFrameCore(hwnd, frame, postRender: false);
#endif
        }

        [Conditional("DEBUG")]
        public static void StartAdjustProbes(Window window, IntPtr hwnd)
        {
#if DEBUG
            if (window == null || hwnd == IntPtr.Zero)
            {
                return;
            }

            _probeWindow = window;
            _probeHwnd = hwnd;
            _lastCursorState = null;
            _cursorProbeLogged = 0;
            ResetMessageCounts();

            if (_renderProbeTimer == null)
            {
                _renderProbeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                _renderProbeTimer.Tick += RenderProbeTick;
            }
            _renderProbeTimer.Stop();
            _renderProbeTimer.Start();

            if (_cursorProbeTimer == null)
            {
                _cursorProbeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                _cursorProbeTimer.Tick += CursorProbeTick;
            }
            _cursorProbeTimer.Start();

            AttachMessageProbe(hwnd);
            AttachThreadMouseHook();
            ComponentDispatcher.ThreadFilterMessage += FilterProbeMessage;

            LogWindowFacts(hwnd);

            Write("adjust probes started: hwnd=0x" + hwnd.ToString("X")
                + " (post-render frame probe ~300ms after each apply;"
                + " cursor probe samples ownership + capture every 500ms;"
                + " wndproc message probe counts raw WM_* mouse messages;"
                + " a thread-wide WH_MOUSE hook tallies them per target window;"
                + " the WPF filter stage is probed between hook and wndproc)");
#endif
        }

        [Conditional("DEBUG")]
        public static void StopAdjustProbes()
        {
#if DEBUG
            if (_renderProbeTimer != null)
            {
                _renderProbeTimer.Stop();
            }
            if (_cursorProbeTimer != null)
            {
                _cursorProbeTimer.Stop();
            }
            if (_probeWindow != null)
            {
                LogMessageCounts();
                Write("adjust probes stopped");
            }
            DetachMessageProbe();
            DetachThreadMouseHook();
            ComponentDispatcher.ThreadFilterMessage -= FilterProbeMessage;

            _probeWindow = null;
            _probeHwnd = IntPtr.Zero;
            _armedFrames.Clear();
            _lastCursorState = null;
#endif
        }

        [Conditional("DEBUG")]
        public static void AttachWindowInputProbe(Window window)
        {
#if DEBUG
            if (window == null || ReferenceEquals(_inputProbeWindow, window))
            {
                return;
            }

            DetachWindowInputProbe(_inputProbeWindow);
            _inputProbeWindow = window;
            window.PreviewMouseLeftButtonDown += WindowProbe_Down;
            window.PreviewMouseMove += WindowProbe_Move;
            window.PreviewMouseLeftButtonUp += WindowProbe_Up;
            Write("window input probe attached (preview events; fires whenever mouse input reaches the overlay window at all)");
#endif
        }

        [Conditional("DEBUG")]
        public static void DetachWindowInputProbe(Window window)
        {
#if DEBUG
            if (window == null || !ReferenceEquals(_inputProbeWindow, window))
            {
                return;
            }

            window.PreviewMouseLeftButtonDown -= WindowProbe_Down;
            window.PreviewMouseMove -= WindowProbe_Move;
            window.PreviewMouseLeftButtonUp -= WindowProbe_Up;
            _inputProbeWindow = null;
            Write("window input probe detached");
#endif
        }

#if DEBUG
        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x00000020;
        private const int WsExTopmost = 0x00000008;
        private const uint GaRoot = 2;
        private const uint GwHwndPrev = 3;
        private const int MdtEffectiveDpi = 0;
        private const int MaxCursorProbeLogs = 12;

        private const int WmSetCursor = 0x0020;
        private const int WmMouseActivate = 0x0021;
        private const int WmNcHitTest = 0x0084;
        private const int WmMouseMove = 0x0200;
        private const int WmLButtonDown = 0x0201;
        private const int WmLButtonUp = 0x0202;
        private const int WmCaptureChanged = 0x0215;
        private const int WmNcMouseMove = 0x00A0;
        private const int WmNcLButtonDown = 0x00A1;
        private const int WmNcLButtonUp = 0x00A2;
        private const int WhMouse = 7;
        private const int GwlStyle = -16;
        private const int GwlHwndParent = -8;
        private const int WsVisible = 0x10000000;
        private const int WsDisabled = 0x08000000;

        // Tally slot order shared by _hookTallies and the filter-stage counts.
        private const int TallyMouseMove = 0;
        private const int TallyLButtonDown = 1;
        private const int TallyLButtonUp = 2;
        private const int TallyNcMouseMove = 3;
        private const int TallyNcLButtonDown = 4;
        private const int TallyNcLButtonUp = 5;

        private sealed class ArmedFrame
        {
            public string Label;
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public bool TakesMouse;
            public double CanvasX;
            public double CanvasY;
            public double BoxWidth;
            public double BoxHeight;
        }

        private static readonly List<ArmedFrame> _armedFrames = new List<ArmedFrame>();
        private static Window _inputProbeWindow;
        private static Window _probeWindow;
        private static IntPtr _probeHwnd;
        private static DispatcherTimer _renderProbeTimer;
        private static DispatcherTimer _cursorProbeTimer;
        private static string _lastCursorState;
        private static int _cursorProbeLogged;

        private static HwndSource _messageProbeSource;
        private static HwndSourceHook _messageProbeHook;
        private static int _ncHitTestCount;
        private static int _mouseMoveCount;
        private static int _lButtonDownCount;
        private static int _lButtonUpCount;
        private static int _setCursorCount;
        private static int _mouseActivateCount;
        private static int _captureChangedCount;
        private static bool _loggedFirstNcHitTest;
        private static bool _loggedFirstMouseMove;
        private static bool _loggedFirstSetCursor;
        private static int _ncMouseMoveCount;
        private static int _ncLButtonDownCount;
        private static int _ncLButtonUpCount;
        private static bool _loggedFirstNcMouseMove;
        private static IntPtr _threadMouseHook;
        private static readonly HookProc ThreadMouseHookProcDelegate = ThreadMouseHookCallback;
        private static int _hookMouseMoveCount;
        private static int _hookLButtonDownCount;
        private static int _hookLButtonUpCount;
        private static int _hookNcMouseMoveCount;
        private static int _hookNcLButtonDownCount;
        private static bool _loggedFirstHookMouseMove;
        private static bool _loggedFirstHookLButtonDown;
        private static bool _loggedFirstHookNcMouseMove;
        private static int _hookNcLButtonUpCount;
        private static readonly Dictionary<IntPtr, int[]> _hookTallies = new Dictionary<IntPtr, int[]>();
        private static readonly HashSet<IntPtr> _hookTargetsIdentified = new HashSet<IntPtr>();
        private static readonly int[] _filterOverlayTally = new int[6];
        private static int _filterOtherTotal;
        private static bool _loggedFirstFilterMouseMove;
        private static int _setCursorForUsCount;
        private static bool _loggedFirstSetCursorOther;
        private static bool _windowFactsDisabled;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Win32Rect rect);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Win32Point point);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetCursorPos(out Win32Point point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetCapture();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetLayeredWindowAttributes(
            IntPtr hwnd,
            out byte alpha,
            out uint colorKey,
            out uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowEnabled(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            HookProc proc,
            IntPtr module,
            uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hook,
            int code,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        private static extern int GetDpiForSystem();

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(
            IntPtr hdc,
            IntPtr clipRect,
            MonitorEnumProc proc,
            IntPtr lParam);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(
            IntPtr hmonitor,
            int dpiType,
            out uint dpiX,
            out uint dpiY);

        private delegate bool MonitorEnumProc(
            IntPtr monitor,
            IntPtr hdc,
            ref Win32Rect rect,
            IntPtr data);

        private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseHookStruct
        {
            public Win32Point Point;
            public IntPtr Hwnd;
            public uint HitTestCode;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GuiThreadInfo
        {
            public int Size;
            public uint Flags;
            public IntPtr Active;
            public IntPtr Focus;
            public IntPtr Capture;
            public IntPtr MenuOwner;
            public IntPtr MoveSize;
            public IntPtr Caret;
            public Win32Rect CaretRect;
        }

        private static void WindowProbe_Down(object sender, MouseButtonEventArgs e)
        {
            NoteInput(sender, "preview-down", e);
        }

        private static void WindowProbe_Move(object sender, MouseEventArgs e)
        {
            NoteInput(sender, "preview-move", e);
        }

        private static void WindowProbe_Up(object sender, MouseButtonEventArgs e)
        {
            NoteInput(sender, "preview-up", e);
        }

        private static void NoteInput(object sender, string kind, MouseEventArgs e)
        {
            var window = sender as Window;
            if (window == null)
            {
                return;
            }

            Point position = e.GetPosition(window);
            RegionAdjustTrace.NoteWindowInput(kind, position.X, position.Y);
        }

        private static bool HasFlag(int exStyle)
        {
            return (exStyle & WsExTransparent) != 0;
        }

        private static void RestartRenderProbe()
        {
            if (_renderProbeTimer != null)
            {
                _renderProbeTimer.Stop();
                _renderProbeTimer.Start();
            }
        }

        private static void RenderProbeTick(object sender, EventArgs e)
        {
            _renderProbeTimer.Stop();
            if (_probeHwnd == IntPtr.Zero || _armedFrames.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _armedFrames.Count; i++)
            {
                ProbeFrameCore(_probeHwnd, _armedFrames[i], postRender: true);
            }
        }

        private static void ProbeFrameCore(IntPtr hwnd, ArmedFrame frame, bool postRender)
        {
            int centerX = frame.X + frame.Width / 2;
            int centerY = frame.Y + frame.Height / 2;
            string hitInfo;
            bool centerIsOurWindow = false;
            if (hwnd == IntPtr.Zero)
            {
                hitInfo = "hit=skipped(hwnd-zero)";
            }
            else
            {
                IntPtr hit = WindowFromPoint(new Win32Point { X = centerX, Y = centerY });
                if (hit == IntPtr.Zero)
                {
                    hitInfo = "hit=NULL(no window owns the point)";
                }
                else
                {
                    IntPtr root = GetAncestor(hit, GaRoot);
                    centerIsOurWindow = hit == hwnd || root == hwnd;
                    hitInfo = "hit=0x" + hit.ToString("X")
                        + " root=0x" + root.ToString("X")
                        + " class='" + DescribeWindow(hit) + "'"
                        + (centerIsOurWindow ? "" : " rootClass='" + DescribeWindow(root) + "'");
                }
            }

            string verdict = hwnd == IntPtr.Zero
                ? "unknown"
                : centerIsOurWindow ? "our-window" : "FOREIGN-WINDOW";
            Write("frame '" + frame.Label + "'" + (postRender ? " (post-render)" : "") + ":"
                + " rect=" + frame.X + "," + frame.Y + "," + frame.Width + "," + frame.Height
                + " canvas=(" + frame.CanvasX.ToString("0.##") + "," + frame.CanvasY.ToString("0.##") + ")"
                + " box=" + frame.BoxWidth.ToString("0.##") + "x" + frame.BoxHeight.ToString("0.##")
                + " takesMouse=" + frame.TakesMouse
                + " centerPhysical=(" + centerX + "," + centerY + ")"
                + " " + hitInfo
                + " verdict=" + verdict);

            if (postRender)
            {
                RegionAdjustTrace.NoteFrameProbePostRender(frame.TakesMouse, centerIsOurWindow);
            }
        }

        private static void CursorProbeTick(object sender, EventArgs e)
        {
            if (_probeHwnd == IntPtr.Zero)
            {
                return;
            }

            Win32Point cursor;
            if (!GetCursorPos(out cursor))
            {
                return;
            }

            var containing = new StringBuilder();
            bool insideInteractive = false;
            for (int i = 0; i < _armedFrames.Count; i++)
            {
                ArmedFrame frame = _armedFrames[i];
                if (cursor.X < frame.X || cursor.X > frame.X + frame.Width
                    || cursor.Y < frame.Y || cursor.Y > frame.Y + frame.Height)
                {
                    continue;
                }
                if (containing.Length > 0)
                {
                    containing.Append("+");
                }
                containing.Append(frame.Label);
                if (frame.TakesMouse)
                {
                    containing.Append("*");
                    insideInteractive = true;
                }
            }

            IntPtr hit = WindowFromPoint(cursor);
            IntPtr root = hit == IntPtr.Zero ? IntPtr.Zero : GetAncestor(hit, GaRoot);
            bool ours = hit == _probeHwnd || root == _probeHwnd;
            Win32Rect ourRect;
            bool insideOurRect = GetWindowRect(_probeHwnd, out ourRect)
                && cursor.X >= ourRect.Left && cursor.X <= ourRect.Right
                && cursor.Y >= ourRect.Top && cursor.Y <= ourRect.Bottom;

            RegionAdjustTrace.NoteCursorProbe(insideInteractive, ours);

            IntPtr threadCapture = GetCapture();
            var guiInfo = new GuiThreadInfo { Size = Marshal.SizeOf(typeof(GuiThreadInfo)) };
            bool haveGui = GetGUIThreadInfo(GetCurrentThreadId(), ref guiInfo);
            string wpfCapture = DescribeInputElement(Mouse.Captured);
            string directlyOver = DescribeInputElement(Mouse.DirectlyOver);

            string state = ours + "|" + hit + "|" + containing + "|" + insideOurRect
                + "|" + threadCapture + "|" + wpfCapture
                + "|" + (haveGui ? guiInfo.Capture + "/" + guiInfo.MoveSize + "/" + guiInfo.MenuOwner : "n/a");
            if (state == _lastCursorState || _cursorProbeLogged >= MaxCursorProbeLogs)
            {
                return;
            }
            _lastCursorState = state;
            _cursorProbeLogged++;

            Write("cursor probe: pos=(" + cursor.X + "," + cursor.Y + ")"
                + " frames='" + (containing.Length == 0 ? "<none>" : containing.ToString()) + "'"
                + " insideOurRect=" + insideOurRect
                + " owner=" + (ours ? "OURS" : "FOREIGN")
                + " hit=0x" + hit.ToString("X")
                + (hit == _probeHwnd ? "(overlay)" : root == _probeHwnd ? "(root=overlay)" : "")
                + " capture=0x" + threadCapture.ToString("X")
                + (haveGui
                    ? " guiCapture=0x" + guiInfo.Capture.ToString("X")
                        + " guiMoveSize=0x" + guiInfo.MoveSize.ToString("X")
                        + " guiMenu=0x" + guiInfo.MenuOwner.ToString("X")
                    : " gui=unavailable")
                + " wpfCapture=" + wpfCapture
                + " over=" + directlyOver
                + (ours
                    ? ""
                    : " hitClass='" + DescribeWindow(hit) + "'"
                        + " rootClass='" + DescribeWindow(root) + "'"
                        + " aboveUs=" + DescribeWindowsAbove(_probeHwnd)));
        }

        /// <summary>
        /// Mouse capture redirects real input while WindowFromPoint ignores it:
        /// a foreign capture is the classic cause of "probe says OURS, yet no
        /// mouse event ever arrives". GetCapture only sees captures held by
        /// windows on our own thread; Mouse.Captured covers the WPF-level
        /// leak of the same thing.
        /// </summary>
        private static string DescribeInputElement(IInputElement element)
        {
            if (element == null)
            {
                return "<null>";
            }
            return element.GetType().Name;
        }

        private static void ResetMessageCounts()
        {
            _ncHitTestCount = 0;
            _mouseMoveCount = 0;
            _lButtonDownCount = 0;
            _lButtonUpCount = 0;
            _setCursorCount = 0;
            _mouseActivateCount = 0;
            _captureChangedCount = 0;
            _loggedFirstNcHitTest = false;
            _loggedFirstMouseMove = false;
            _loggedFirstSetCursor = false;
            _ncMouseMoveCount = 0;
            _ncLButtonDownCount = 0;
            _ncLButtonUpCount = 0;
            _loggedFirstNcMouseMove = false;
            _hookMouseMoveCount = 0;
            _hookLButtonDownCount = 0;
            _hookLButtonUpCount = 0;
            _hookNcMouseMoveCount = 0;
            _hookNcLButtonDownCount = 0;
            _loggedFirstHookMouseMove = false;
            _loggedFirstHookLButtonDown = false;
            _loggedFirstHookNcMouseMove = false;
            _hookNcLButtonUpCount = 0;
            _hookTallies.Clear();
            _hookTargetsIdentified.Clear();
            for (int i = 0; i < _filterOverlayTally.Length; i++)
            {
                _filterOverlayTally[i] = 0;
            }
            _filterOtherTotal = 0;
            _loggedFirstFilterMouseMove = false;
            _setCursorForUsCount = 0;
            _loggedFirstSetCursorOther = false;
            _windowFactsDisabled = false;
        }

        private static void AttachMessageProbe(IntPtr hwnd)
        {
            DetachMessageProbe();
            HwndSource source = HwndSource.FromHwnd(hwnd);
            if (source == null)
            {
                Write("hwnd message probe: skipped, no HwndSource for hwnd");
                return;
            }

            _messageProbeSource = source;
            _messageProbeHook = MessageProbeHook;
            source.AddHook(_messageProbeHook);
            Write("hwnd message probe attached (raw WM_* mouse messages at the wndproc, before WPF input processing)");
        }

        private static void DetachMessageProbe()
        {
            if (_messageProbeSource != null)
            {
                _messageProbeSource.RemoveHook(_messageProbeHook);
                _messageProbeSource = null;
                _messageProbeHook = null;
            }
        }

        private static void AttachThreadMouseHook()
        {
            DetachThreadMouseHook();
            _threadMouseHook = SetWindowsHookEx(
                WhMouse, ThreadMouseHookProcDelegate, IntPtr.Zero, GetCurrentThreadId());
            if (_threadMouseHook == IntPtr.Zero)
            {
                Write("thread mouse hook: attach failed lastError=" + Marshal.GetLastWin32Error());
                return;
            }
            Write("thread mouse hook attached (WH_MOUSE on the UI thread; observes every mouse message for this thread before dispatch, ahead of any older hook)");
        }

        private static void DetachThreadMouseHook()
        {
            if (_threadMouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_threadMouseHook);
                _threadMouseHook = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Observation only: counts and always calls CallNextHookEx. Thread
        /// hooks run newest-installed-first, so this sees each message before
        /// any hook installed earlier can swallow it; a move seen here but
        /// missing at the wndproc is the smoking gun for an eating hook or
        /// filter on this thread.
        /// </summary>
        private static IntPtr ThreadMouseHookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WmMouseMove || msg == WmLButtonDown || msg == WmLButtonUp
                    || msg == WmNcMouseMove || msg == WmNcLButtonDown || msg == WmNcLButtonUp)
                {
                    var info = (MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookStruct));
                    NoteHookMessage(msg, ref info);
                }
            }

            return CallNextHookEx(_threadMouseHook, code, wParam, lParam);
        }

        private static void NoteHookMessage(int msg, ref MouseHookStruct info)
        {
            int[] tally;
            if (!_hookTallies.TryGetValue(info.Hwnd, out tally))
            {
                tally = new int[6];
                _hookTallies[info.Hwnd] = tally;
            }
            if (!_hookTargetsIdentified.Contains(info.Hwnd))
            {
                _hookTargetsIdentified.Add(info.Hwnd);
                LogHookTargetIdentity(info.Hwnd);
            }

            string target = "0x" + info.Hwnd.ToString("X")
                + (info.Hwnd == _probeHwnd ? "(overlay)" : "(other-window-on-this-thread)");
            string point = "(" + info.Point.X + "," + info.Point.Y + ")";
            switch (msg)
            {
                case WmMouseMove:
                    _hookMouseMoveCount++;
                    tally[TallyMouseMove]++;
                    if (!_loggedFirstHookMouseMove)
                    {
                        _loggedFirstHookMouseMove = true;
                        Write("thread hook: first WM_MOUSEMOVE target=" + target + " pt=" + point);
                    }
                    break;
                case WmLButtonDown:
                    _hookLButtonDownCount++;
                    tally[TallyLButtonDown]++;
                    if (!_loggedFirstHookLButtonDown)
                    {
                        _loggedFirstHookLButtonDown = true;
                        Write("thread hook: WM_LBUTTONDOWN target=" + target + " pt=" + point);
                    }
                    break;
                case WmLButtonUp:
                    _hookLButtonUpCount++;
                    tally[TallyLButtonUp]++;
                    break;
                case WmNcMouseMove:
                    _hookNcMouseMoveCount++;
                    tally[TallyNcMouseMove]++;
                    if (!_loggedFirstHookNcMouseMove)
                    {
                        _loggedFirstHookNcMouseMove = true;
                        Write("thread hook: first WM_NCMOUSEMOVE target=" + target + " pt=" + point);
                    }
                    break;
                case WmNcLButtonDown:
                    _hookNcLButtonDownCount++;
                    tally[TallyNcLButtonDown]++;
                    Write("thread hook: WM_NCLBUTTONDOWN target=" + target + " pt=" + point);
                    break;
                case WmNcLButtonUp:
                    _hookNcLButtonUpCount++;
                    tally[TallyNcLButtonUp]++;
                    break;
            }
        }

        /// <summary>
        /// Names an unknown hook target once: which window on this thread is
        /// receiving the mouse input the overlay never sees (class, title,
        /// rect, styles, enabled/visible, parent) — enough to recognize "the
        /// app's own settings window", "a WPF popup", or anything injected.
        /// </summary>
        private static void LogHookTargetIdentity(IntPtr hwnd)
        {
            Win32Rect rect;
            GetWindowRect(hwnd, out rect);
            Write("thread hook target first-seen: hwnd=0x" + hwnd.ToString("X")
                + (hwnd == _probeHwnd ? "(overlay)" : "(other)")
                + " class='" + DescribeWindow(hwnd) + "'"
                + " rect=(" + rect.Left + "," + rect.Top
                + " " + (rect.Right - rect.Left) + "x" + (rect.Bottom - rect.Top) + ")"
                + " style=0x" + GetWindowLong(hwnd, GwlStyle).ToString("X")
                + " exStyle=0x" + GetWindowLong(hwnd, GwlExStyle).ToString("X")
                + " enabled=" + IsWindowEnabled(hwnd)
                + " visible=" + IsWindowVisible(hwnd)
                + " parent=0x" + GetWindowLong(hwnd, GwlHwndParent).ToString("X"));
        }

        /// <summary>
        /// Observation only — never touches the message. ComponentDispatcher
        /// raises ThreadFilterMessage when the WPF pump retrieves a message,
        /// before any IMessageFilter may consume it and before the wndproc:
        /// a mouse message tallied by the hook AND seen here AND still missing
        /// at the wndproc was eaten by a WPF message filter; seen by the hook
        /// but not here, it vanished between the hook and retrieval (a
        /// pre-existing native hook or a retrieval-time discard).
        /// </summary>
        private static void FilterProbeMessage(ref MSG msg, ref bool handled)
        {
            int message = msg.message;
            if (message != WmMouseMove && message != WmLButtonDown && message != WmLButtonUp
                && message != WmNcMouseMove && message != WmNcLButtonDown && message != WmNcLButtonUp)
            {
                return;
            }

            if (msg.hwnd == _probeHwnd)
            {
                switch (message)
                {
                    case WmMouseMove: _filterOverlayTally[TallyMouseMove]++; break;
                    case WmLButtonDown: _filterOverlayTally[TallyLButtonDown]++; break;
                    case WmLButtonUp: _filterOverlayTally[TallyLButtonUp]++; break;
                    case WmNcMouseMove: _filterOverlayTally[TallyNcMouseMove]++; break;
                    case WmNcLButtonDown: _filterOverlayTally[TallyNcLButtonDown]++; break;
                    case WmNcLButtonUp: _filterOverlayTally[TallyNcLButtonUp]++; break;
                }
                if (!_loggedFirstFilterMouseMove && message == WmMouseMove)
                {
                    _loggedFirstFilterMouseMove = true;
                    Write("filter stage: first WM_MOUSEMOVE hwnd=0x" + msg.hwnd.ToString("X")
                        + "(overlay) pt=(" + msg.pt_x + "," + msg.pt_y + ")"
                        + " (seen at WPF retrieval, before any IMessageFilter and before the wndproc)");
                }
            }
            else
            {
                _filterOtherTotal++;
            }
        }

        private static void LogWindowFacts(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GwlStyle);
            _windowFactsDisabled = (style & WsDisabled) != 0;
            byte alpha;
            uint colorKey;
            uint flags;
            bool hasAttrs = GetLayeredWindowAttributes(hwnd, out alpha, out colorKey, out flags);
            string layered = hasAttrs
                ? "SLWA alpha=" + alpha + " colorKey=0x" + colorKey.ToString("X")
                    + " flags=0x" + flags.ToString("X")
                : "per-pixel(ULW)/no-attrs lastError=" + Marshal.GetLastWin32Error();
            Write("window facts: style=0x" + style.ToString("X")
                + " visible=" + ((style & WsVisible) != 0)
                + " disabled=" + ((style & WsDisabled) != 0)
                + " isWindowEnabled=" + IsWindowEnabled(hwnd)
                + " isWindowVisible=" + IsWindowVisible(hwnd)
                + " layered=" + layered);
        }

        private static void LogMessageCounts()
        {
            Write("hwnd msg counts: nchittest=" + _ncHitTestCount
                + " mousemove=" + _mouseMoveCount
                + " lbuttondown=" + _lButtonDownCount
                + " lbuttonup=" + _lButtonUpCount
                + " ncmousemove=" + _ncMouseMoveCount
                + " nclbuttondown=" + _ncLButtonDownCount
                + " nclbuttonup=" + _ncLButtonUpCount
                + " setcursor=" + _setCursorCount
                + "(for-us=" + _setCursorForUsCount + ")"
                + " mouseactivate=" + _mouseActivateCount
                + " capturechanged=" + _captureChangedCount);

            foreach (KeyValuePair<IntPtr, int[]> entry in _hookTallies)
            {
                int[] counts = entry.Value;
                Write("thread hook per-target: hwnd=0x" + entry.Key.ToString("X")
                    + (entry.Key == _probeHwnd ? "(overlay)" : "(other — identity logged at first-seen)")
                    + " mousemove=" + counts[TallyMouseMove]
                    + " lbuttondown=" + counts[TallyLButtonDown]
                    + " lbuttonup=" + counts[TallyLButtonUp]
                    + " ncmousemove=" + counts[TallyNcMouseMove]
                    + " nclbuttondown=" + counts[TallyNcLButtonDown]
                    + " nclbuttonup=" + counts[TallyNcLButtonUp]);
            }
            if (_hookTallies.Count == 0)
            {
                Write("thread hook per-target: <no mouse message reached any window on this thread>");
            }

            Write("filter stage counts (overlay-addressed): mousemove=" + _filterOverlayTally[TallyMouseMove]
                + " lbuttondown=" + _filterOverlayTally[TallyLButtonDown]
                + " lbuttonup=" + _filterOverlayTally[TallyLButtonUp]
                + " ncmousemove=" + _filterOverlayTally[TallyNcMouseMove]
                + " nclbuttondown=" + _filterOverlayTally[TallyNcLButtonDown]
                + " nclbuttonup=" + _filterOverlayTally[TallyNcLButtonUp]
                + "; other-windows=" + _filterOtherTotal);

            int[] overlayTally;
            _hookTallies.TryGetValue(_probeHwnd, out overlayTally);
            int hookOverlayMoves = overlayTally == null ? 0 : overlayTally[TallyMouseMove];
            int hookOverlayDowns = overlayTally == null ? 0 : overlayTally[TallyLButtonDown];
            int hookOverlayNcMoves = overlayTally == null ? 0 : overlayTally[TallyNcMouseMove];
            int hookOverlayNcDowns = overlayTally == null ? 0 : overlayTally[TallyNcLButtonDown];
            bool hookSawOverlayInput = hookOverlayMoves > 0 || hookOverlayDowns > 0
                || hookOverlayNcMoves > 0 || hookOverlayNcDowns > 0;
            if (hookSawOverlayInput && _mouseMoveCount == 0 && _lButtonDownCount == 0)
            {
                if (_filterOverlayTally[TallyMouseMove] > 0 || _filterOverlayTally[TallyLButtonDown] > 0
                    || _filterOverlayTally[TallyNcMouseMove] > 0 || _filterOverlayTally[TallyNcLButtonDown] > 0)
                {
                    Write("probe verdict: mouse messages WERE addressed to the overlay and reached WPF retrieval,"
                        + " but never the wndproc — an IMessageFilter on this thread consumed them");
                }
                else if (_windowFactsDisabled)
                {
                    Write("probe verdict: mouse messages WERE addressed to the overlay (the hook saw them)"
                        + " but vanished before WPF retrieval — the overlay was WS_DISABLED (see window facts),"
                        + " and the kernel drops input addressed to a disabled window before retrieval");
                }
                else
                {
                    Write("probe verdict: mouse messages WERE addressed to the overlay (the hook saw them)"
                        + " but vanished before WPF retrieval — a pre-existing native hook on this thread or a retrieval-time discard ate them");
                }
            }
            else if (!hookSawOverlayInput && _setCursorForUsCount > 0)
            {
                Write("probe verdict: the OS sent WM_SETCURSOR to the overlay " + _setCursorForUsCount
                    + " times yet not one mouse message was ever addressed to it —"
                    + (_windowFactsDisabled
                        ? " the overlay was WS_DISABLED (see window facts): the kernel refuses to route input to a disabled window"
                        : " the kernel refuses to route input to this window (hit code from WM_SETCURSOR is authoritative)"));
            }
        }

        /// <summary>
        /// Observation only — never consumes a message (always returns zero and
        /// leaves handled=false). WM_NCHITTEST and WM_SETCURSOR follow the
        /// window under the cursor rather than the capture window, so they can
        /// arrive while captured-away WM_MOUSEMOVE does not; that split is the
        /// signature of "another window holds mouse capture".
        /// </summary>
        private static IntPtr MessageProbeHook(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            switch (msg)
            {
                case WmNcHitTest:
                    _ncHitTestCount++;
                    if (!_loggedFirstNcHitTest)
                    {
                        _loggedFirstNcHitTest = true;
                        Write("hwnd msg: first WM_NCHITTEST screen=" + FormatLParamPoint(lParam)
                            + " (OS queried our window under the cursor)");
                    }
                    break;
                case WmMouseMove:
                    _mouseMoveCount++;
                    if (!_loggedFirstMouseMove)
                    {
                        _loggedFirstMouseMove = true;
                        Write("hwnd msg: first WM_MOUSEMOVE client=" + FormatLParamPoint(lParam));
                    }
                    break;
                case WmLButtonDown:
                    _lButtonDownCount++;
                    Write("hwnd msg: WM_LBUTTONDOWN client=" + FormatLParamPoint(lParam));
                    break;
                case WmLButtonUp:
                    _lButtonUpCount++;
                    Write("hwnd msg: WM_LBUTTONUP client=" + FormatLParamPoint(lParam));
                    break;
                case WmNcMouseMove:
                    _ncMouseMoveCount++;
                    if (!_loggedFirstNcMouseMove)
                    {
                        _loggedFirstNcMouseMove = true;
                        Write("hwnd msg: first WM_NCMOUSEMOVE screen=" + FormatLParamPoint(lParam));
                    }
                    break;
                case WmNcLButtonDown:
                    _ncLButtonDownCount++;
                    Write("hwnd msg: WM_NCLBUTTONDOWN screen=" + FormatLParamPoint(lParam));
                    break;
                case WmNcLButtonUp:
                    _ncLButtonUpCount++;
                    Write("hwnd msg: WM_NCLBUTTONUP screen=" + FormatLParamPoint(lParam));
                    break;
                case WmSetCursor:
                    _setCursorCount++;
                    long detail = lParam.ToInt64();
                    int hitCode = (short)(detail & 0xFFFF);
                    int trigger = (int)((detail >> 16) & 0xFFFF);
                    if (wParam == _probeHwnd)
                    {
                        _setCursorForUsCount++;
                        if (!_loggedFirstSetCursor)
                        {
                            _loggedFirstSetCursor = true;
                            Win32Point probePoint;
                            IntPtr fromPoint = GetCursorPos(out probePoint)
                                ? WindowFromPoint(probePoint)
                                : IntPtr.Zero;
                            Write("hwnd msg: first WM_SETCURSOR containing=0x" + wParam.ToString("X")
                                + "(us)"
                                + " hit=" + DescribeHitCode(hitCode)
                                + " trigger=0x" + trigger.ToString("X")
                                + " windowFromPoint=0x" + fromPoint.ToString("X")
                                + (fromPoint == _probeHwnd ? "(us)" : "(NOT-US — the two hit tests disagree)")
                                + " (what the OS was about to deliver to the window under the cursor)");
                        }
                    }
                    else if (!_loggedFirstSetCursorOther)
                    {
                        _loggedFirstSetCursorOther = true;
                        Write("hwnd msg: first WM_SETCURSOR containing=0x" + wParam.ToString("X")
                            + "(NOT-US — someone forwarded it to us)"
                            + " hit=" + DescribeHitCode(hitCode)
                            + " trigger=0x" + trigger.ToString("X"));
                    }
                    break;
                case WmMouseActivate:
                    _mouseActivateCount++;
                    Write("hwnd msg: WM_MOUSEACTIVATE (OS offered activation on press)");
                    break;
                case WmCaptureChanged:
                    _captureChangedCount++;
                    Write("hwnd msg: WM_CAPTURECHANGED newOwner=0x" + lParam.ToString("X"));
                    break;
            }

            return IntPtr.Zero;
        }

        private static string FormatLParamPoint(IntPtr lParam)
        {
            long value = lParam.ToInt64();
            return ((short)(value & 0xFFFF)) + "," + ((short)((value >> 16) & 0xFFFF));
        }

        private static string DescribeHitCode(int hitCode)
        {
            switch (hitCode)
            {
                case -2: return "HTERROR";
                case -1: return "HTTRANSPARENT";
                case 0: return "HTNOWHERE";
                case 1: return "HTCLIENT";
                case 2: return "HTCAPTION";
                case 3: return "HTSYSMENU";
                default: return "HT(" + hitCode + ")";
            }
        }

        /// <summary>
        /// Names up to three windows directly above ours in z-order, with their
        /// TOPMOST bit — distinguishing "a topmost window covers us" from "our
        /// own transparent pixels pass the click through to what is below".
        /// </summary>
        private static string DescribeWindowsAbove(IntPtr hwnd)
        {
            var chain = new StringBuilder();
            IntPtr current = hwnd;
            for (int i = 0; i < 3; i++)
            {
                IntPtr above = GetWindow(current, GwHwndPrev);
                if (above == IntPtr.Zero)
                {
                    break;
                }
                if (chain.Length > 0)
                {
                    chain.Append(" > ");
                }
                chain.Append("0x" + above.ToString("X"))
                    .Append((GetWindowLong(above, GwlExStyle) & WsExTopmost) != 0 ? "(topmost)" : "(normal)")
                    .Append(" '").Append(DescribeWindow(above)).Append("'");
                current = above;
            }
            return chain.Length == 0 ? "<none-we-are-topmost>" : chain.ToString();
        }

        private static string DescribeWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return "<null>";
            }

            var className = new StringBuilder(256);
            GetClassName(hwnd, className, className.Capacity);
            var text = new StringBuilder(256);
            GetWindowText(hwnd, text, text.Capacity);
            return className + " | " + text;
        }

        private static List<string> DescribeMonitors()
        {
            var lines = new List<string>();
            int index = 0;
            MonitorEnumProc collect = delegate(IntPtr monitor, IntPtr hdc, ref Win32Rect rect, IntPtr data)
            {
                uint dpiX;
                uint dpiY;
                int hr = GetDpiForMonitor(monitor, MdtEffectiveDpi, out dpiX, out dpiY);
                string dpi = hr == 0
                    ? dpiX + "x" + dpiY + " (effective scale " + (dpiX / 96.0).ToString("0.###") + ")"
                    : "unavailable (hr=0x" + hr.ToString("X") + ")";
                lines.Add(
                    "monitor[" + index + "]: rectPhysical=(" + rect.Left + "," + rect.Top
                    + " " + (rect.Right - rect.Left) + "x" + (rect.Bottom - rect.Top) + ")"
                    + " dpi=" + dpi);
                index++;
                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, collect, IntPtr.Zero);
            return lines;
        }

        private static void Write(string message)
        {
            Common.Logger.Log.Debug("[AdjustTrace] " + message);
        }
#endif
    }
}
