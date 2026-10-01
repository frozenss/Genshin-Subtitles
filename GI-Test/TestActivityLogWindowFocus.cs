using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using GI_Subtitles.Core.Overlay;
using GI_Subtitles.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GI_Test
{
    /// <summary>
    /// Activity-log window activation: overlay z-order reassert must not grey
    /// the log title bar or drop in-cell selection.
    /// </summary>
    [TestClass]
    public class TestActivityLogWindowFocus
    {
        private const int GwlExStyle = -20;
        private const int WsExTransparent = 0x00000020;
        private const int WsExToolWindow = 0x00000080;
        private const int WsExNoActivate = 0x08000000;
        private const int WsExLayered = 0x00080000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [TestMethod]
        public void ActivityLog_RemainsActive_AfterOverlayReassertTopmost()
        {
            RunOnSta(delegate
            {
                ProbeResult result = RunOverlayProbe(delegate(Window overlay)
                {
                    MainWindow.ReassertOverlayTopmost(new WindowInteropHelper(overlay).Handle);
                });
                AssertStayActive(
                    result,
                    "overlay ReassertOverlayTopmost stole activity-log activation");
            });
        }

        [TestMethod]
        public void ActivityLog_RemainsActive_AfterOverlayBoundsReassign()
        {
            RunOnSta(delegate
            {
                ProbeResult result = RunOverlayProbe(delegate(Window overlay)
                {
                    overlay.Left = overlay.Left;
                    overlay.Top = overlay.Top;
                    overlay.Width = overlay.Width;
                    overlay.Height = overlay.Height;
                });
                AssertStayActive(
                    result,
                    "re-assigning overlay Left/Top/Width/Height stole activity-log activation");
            });
        }

        [TestMethod]
        public void ActivityLog_RemainsActive_AfterOverlaySubtitleTextChange()
        {
            RunOnSta(delegate
            {
                ProbeResult result = RunOverlayProbe(delegate(Window overlay)
                {
                    var box = (TextBox)overlay.Content;
                    box.Text = "updated-subtitle-" + Guid.NewGuid().ToString("N");
                });
                AssertStayActive(
                    result,
                    "updating overlay SubtitleText stole activity-log activation");
            });
        }

        [TestMethod]
        public void ActivityLog_RemainsActive_AndKeepsSelection_AfterRowAppend()
        {
            RunOnSta(delegate
            {
                EnsureApplication();
                LiveOverlaySession session = new LiveOverlaySession(new MemoryOcrIntervalStore());
                AppendRow(session, 0);
                ActivityLogWindow log = OpenLog(session);
                Window overlay = OpenOverlay();
                try
                {
                    ArmLog(log);
                    TextBox cell = FindResultLineTextBox(log.LogList);
                    Assert.IsNotNull(cell, "result line TextBox missing");
                    cell.Focus();
                    Pump(log.Dispatcher);
                    int take = Math.Min(12, cell.Text.Length);
                    cell.Select(0, take);
                    Pump(log.Dispatcher);
                    Assert.IsTrue(cell.SelectionLength > 0, "could not select cell text");
                    Assert.IsTrue(log.IsActive, "activity log was not active before append");

                    AppendRow(session, 1);
                    Pump(log.Dispatcher);
                    Pump(log.Dispatcher);

                    TextBox after = Keyboard.FocusedElement as TextBox;
                    Assert.IsTrue(
                        log.IsActive,
                        "row append deactivated the activity log. " + Snapshot(log, overlay));
                    Assert.IsTrue(
                        log.IsKeyboardFocusWithin,
                        "row append moved keyboard focus out of the activity log. " + Snapshot(log, overlay));
                    Assert.IsNotNull(after, "row append dropped TextBox focus");
                    Assert.IsTrue(
                        after.SelectionLength > 0,
                        "row append cleared in-cell selection");
                }
                finally
                {
                    overlay.Close();
                    ForceClose(log);
                }
            });
        }

        [TestMethod]
        public void ActivityLog_RemainsActive_AfterHintChromeShow()
        {
            RunOnSta(delegate
            {
                EnsureApplication();
                LiveOverlaySession session = new LiveOverlaySession(new MemoryOcrIntervalStore());
                AppendRow(session, 0);
                ActivityLogWindow log = OpenLog(session);
                var chrome = new OverlayHintChrome();
                try
                {
                    ArmLog(log);
                    Assert.IsTrue(log.IsActive, "activity log was not active before hint show");
                    chrome.Show("hint", new Rect(0, 0, 400, 200));
                    Pump(log.Dispatcher);
                    Assert.IsTrue(
                        log.IsActive,
                        "hint chrome Show() deactivated the activity log");
                }
                finally
                {
                    chrome.Close();
                    ForceClose(log);
                }
            });
        }

        private static ProbeResult RunOverlayProbe(Action<Window> overlayAction)
        {
            EnsureApplication();
            LiveOverlaySession session = new LiveOverlaySession(new MemoryOcrIntervalStore());
            AppendRow(session, 0);
            ActivityLogWindow log = OpenLog(session);
            Window overlay = OpenOverlay();
            try
            {
                ArmLog(log);
                TextBox cell = FindResultLineTextBox(log.LogList);
                if (cell != null)
                {
                    cell.Focus();
                    Pump(log.Dispatcher);
                    int take = Math.Min(12, cell.Text.Length);
                    cell.Select(0, take);
                    Pump(log.Dispatcher);
                }

                var before = new ProbeResult
                {
                    LogActive = log.IsActive,
                    OverlayActive = overlay.IsActive,
                    LogFocusWithin = log.IsKeyboardFocusWithin,
                    SelectionLength = cell == null ? 0 : cell.SelectionLength,
                    ForegroundIsLog = GetForegroundWindow() == new WindowInteropHelper(log).Handle,
                    Snapshot = Snapshot(log, overlay)
                };

                overlayAction(overlay);
                Pump(log.Dispatcher);
                Pump(overlay.Dispatcher);

                TextBox afterCell = Keyboard.FocusedElement as TextBox;
                return new ProbeResult
                {
                    Armed = before.LogActive,
                    LogActive = log.IsActive,
                    OverlayActive = overlay.IsActive,
                    LogFocusWithin = log.IsKeyboardFocusWithin,
                    SelectionLength = afterCell == null ? 0 : afterCell.SelectionLength,
                    ForegroundIsLog = GetForegroundWindow() == new WindowInteropHelper(log).Handle,
                    Snapshot = "before: " + before.Snapshot + " | after: " + Snapshot(log, overlay)
                };
            }
            finally
            {
                overlay.Close();
                ForceClose(log);
            }
        }

        private static void AssertStayActive(ProbeResult result, string because)
        {
            Assert.IsTrue(
                result.Armed,
                "could not activate the activity log before the probe (session not foreground?). " + result.Snapshot);
            Assert.IsTrue(result.LogActive, because + ". " + result.Snapshot);
            Assert.IsFalse(
                result.OverlayActive,
                because + " (overlay became IsActive). " + result.Snapshot);
            Assert.IsTrue(
                result.LogFocusWithin,
                because + " (keyboard focus left the log). " + result.Snapshot);
        }

        private static Window OpenOverlay()
        {
            var subtitle = new TextBox
            {
                Text = "overlay-body",
                IsReadOnly = true,
                AcceptsReturn = true,
                IsHitTestVisible = false,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            var overlay = new Window
            {
                Title = "overlay-probe",
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                ShowActivated = false,
                Focusable = false,
                IsHitTestVisible = false,
                Width = 240,
                Height = 80,
                Left = 20,
                Top = 20,
                Content = subtitle
            };
            overlay.SourceInitialized += (sender, args) =>
            {
                IntPtr hwnd = new WindowInteropHelper(overlay).Handle;
                int exStyle = GetWindowLong(hwnd, GwlExStyle);
                SetWindowLong(
                    hwnd,
                    GwlExStyle,
                    exStyle | WsExTransparent | WsExLayered | WsExToolWindow | WsExNoActivate);
            };
            overlay.Show();
            Pump(overlay.Dispatcher);
            return overlay;
        }

        private static ActivityLogWindow OpenLog(LiveOverlaySession session)
        {
            var window = new ActivityLogWindow(session)
            {
                ShowInTaskbar = false,
                Left = 80,
                Top = 80,
                Width = 720,
                Height = 400
            };
            window.Show();
            Pump(window.Dispatcher);
            window.UpdateLayout();
            Pump(window.Dispatcher);
            return window;
        }

        private static void ArmLog(ActivityLogWindow log)
        {
            log.Activate();
            Pump(log.Dispatcher);
            if (!log.IsActive)
            {
                log.Activate();
                Pump(log.Dispatcher);
            }
        }

        private static string Snapshot(Window log, Window overlay)
        {
            IntPtr foreground = GetForegroundWindow();
            IntPtr logHwnd = new WindowInteropHelper(log).Handle;
            IntPtr overlayHwnd = overlay == null
                ? IntPtr.Zero
                : new WindowInteropHelper(overlay).Handle;
            IInputElement focused = Keyboard.FocusedElement;
            string focusedName = focused == null ? "null" : focused.GetType().Name;
            return string.Format(
                "log.IsActive={0} overlay.IsActive={1} log.FocusWithin={2} focused={3} fg=log:{4} overlay:{5}",
                log.IsActive,
                overlay == null ? (bool?)null : overlay.IsActive,
                log.IsKeyboardFocusWithin,
                focusedName,
                foreground == logHwnd,
                overlayHwnd != IntPtr.Zero && foreground == overlayHwnd);
        }

        private static TextBox FindResultLineTextBox(ListView list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                var container = list.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                if (container == null)
                {
                    continue;
                }

                TextBox line = FindResultLineTextBoxIn(container);
                if (line != null)
                {
                    return line;
                }
            }

            return null;
        }

        private static TextBox FindResultLineTextBoxIn(DependencyObject root)
        {
            if (root is TextBox box
                && box.TextWrapping == TextWrapping.Wrap
                && box.DataContext is ActivityLogResultLine)
            {
                return box;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                TextBox child = FindResultLineTextBoxIn(VisualTreeHelper.GetChild(root, i));
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static void AppendRow(LiveOverlaySession session, int index)
        {
            MethodInfo append = typeof(LiveOverlaySession).GetMethod(
                "AppendActivityLogRow",
                BindingFlags.Instance | BindingFlags.NonPublic);
            append.Invoke(
                session,
                new object[]
                {
                    new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc).AddSeconds(index),
                    new[] { OperatorJob.Capture, OperatorJob.Ocr, OperatorJob.Match },
                    ActivityLogScope.Pair,
                    1,
                    true,
                    null,
                    null,
                    "ocr-hello-" + index,
                    "你好世界-" + index,
                    "hello world translation line " + index,
                    false,
                    false,
                    false
                });
        }

        private static void ForceClose(ActivityLogWindow window)
        {
            FieldInfo field = typeof(ActivityLogWindow).GetField(
                "_forceClose",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(window, true);
            window.Close();
        }

        private static void EnsureApplication()
        {
            if (Application.Current == null)
            {
                var app = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri(
                        "pack://application:,,,/GI-Subtitles;component/Resources/Strings.zh-CN.xaml",
                        UriKind.Absolute)
                });
            }
            else if (Application.Current.Resources.MergedDictionaries.Count == 0)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri(
                        "pack://application:,,,/GI-Subtitles;component/Resources/Strings.zh-CN.xaml",
                        UriKind.Absolute)
                });
            }
        }

        private static void Pump(Dispatcher dispatcher)
        {
            var frame = new DispatcherFrame();
            dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(delegate { frame.Continue = false; }));
            Dispatcher.PushFrame(frame);
        }

        private static readonly object StaGate = new object();
        private static Dispatcher _staDispatcher;
        private static Exception _staStartFailure;

        private static void RunOnSta(Action action)
        {
            EnsureStaDispatcher();
            Exception failure = null;
            _staDispatcher.Invoke(delegate
            {
                try
                {
                    EnsureApplication();
                    action();
                }
                catch (Exception e)
                {
                    failure = e;
                }
            });
            if (failure != null)
            {
                throw new AssertFailedException(failure.Message, failure);
            }
        }

        private static void EnsureStaDispatcher()
        {
            lock (StaGate)
            {
                if (_staDispatcher != null)
                {
                    return;
                }

                if (Application.Current != null)
                {
                    _staDispatcher = Application.Current.Dispatcher;
                    return;
                }

                var ready = new ManualResetEvent(false);
                var thread = new Thread(delegate()
                {
                    try
                    {
                        EnsureApplication();
                        _staDispatcher = Dispatcher.CurrentDispatcher;
                    }
                    catch (Exception e)
                    {
                        _staStartFailure = e;
                    }
                    finally
                    {
                        ready.Set();
                    }

                    if (_staDispatcher != null)
                    {
                        Dispatcher.Run();
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
                ready.WaitOne();
                if (_staStartFailure != null)
                {
                    throw new AssertFailedException(_staStartFailure.Message, _staStartFailure);
                }

                Assert.IsNotNull(_staDispatcher, "STA dispatcher failed to start");
            }
        }

        private sealed class ProbeResult
        {
            public bool Armed;
            public bool LogActive;
            public bool OverlayActive;
            public bool LogFocusWithin;
            public int SelectionLength;
            public bool ForegroundIsLog;
            public string Snapshot;
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
