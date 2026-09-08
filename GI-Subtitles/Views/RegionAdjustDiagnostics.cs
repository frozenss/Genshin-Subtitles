using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using GI_Subtitles.Core.Overlay;

namespace GI_Subtitles.Views
{
    /// <summary>
    /// Debug-only Win32 diagnostics for the region-adjust pipeline (issue #37):
    /// the exStyle switch around WS_EX_TRANSPARENT, a DPI/geometry snapshot,
    /// and a WindowFromPoint probe at each adjust frame's center. Every public
    /// method is [Conditional("DEBUG")] and every body — including the Win32
    /// declarations — is compiled out unless DEBUG is defined, so Release
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
        public static void SnapshotEnvironment(Window window, IntPtr hwnd, double scale)
        {
#if DEBUG
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

            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
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
            Write("frame '" + label + "': rect=" + rect.ToCsv()
                + " canvas=(" + canvasPoint.X.ToString("0.##") + "," + canvasPoint.Y.ToString("0.##") + ")"
                + " box=" + boxWidth.ToString("0.##") + "x" + boxHeight.ToString("0.##")
                + " takesMouse=" + takesMouse
                + " centerPhysical=(" + centerX + "," + centerY + ")"
                + " " + hitInfo
                + " verdict=" + verdict);

            RegionAdjustTrace.NoteFrameProbe(takesMouse, centerIsOurWindow);
#endif
        }

        [Conditional("DEBUG")]
        public static void AttachWindowInputProbe(Window window)
        {
#if DEBUG
            if (window == null || ReferenceEquals(_probedWindow, window))
            {
                return;
            }

            DetachWindowInputProbe(_probedWindow);
            _probedWindow = window;
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
            if (window == null || !ReferenceEquals(_probedWindow, window))
            {
                return;
            }

            window.PreviewMouseLeftButtonDown -= WindowProbe_Down;
            window.PreviewMouseMove -= WindowProbe_Move;
            window.PreviewMouseLeftButtonUp -= WindowProbe_Up;
            _probedWindow = null;
            Write("window input probe detached");
#endif
        }

#if DEBUG
        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x00000020;
        private const uint GaRoot = 2;
        private const int MdtEffectiveDpi = 0;

        private static Window _probedWindow;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Win32Rect rect);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Win32Point point);

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
