using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GI_Subtitles.Core.Config;
using GI_Subtitles.Core.Overlay;

namespace GI_Subtitles.Views
{
    public partial class ActivityLogWindow : Window
    {
        private readonly LiveOverlaySession _session;
        private readonly ObservableCollection<ActivityLogRowView> _rows = new ObservableCollection<ActivityLogRowView>();
        private readonly List<ActivityLogRow> _rowSources = new List<ActivityLogRow>();
        private ScrollViewer _scrollViewer;
        private bool _followTail = true;
        private bool _forceClose;
        private bool _opened;
        private ActivityLogRowFilter _filter = new ActivityLogRowFilter(ReadLogDenoise());
        private int _anchorIndex = -1;

        public ActivityLogWindow(LiveOverlaySession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _session = session;
            InitializeComponent();
            LogList.ItemsSource = _rows;
            Loaded += OnLoaded;
            Closing += OnClosing;
            Application.Current.Exit += OnAppExit;
            _session.ActivityLogChanged += OnActivityLogChanged;
        }

        public void ShowOrFocus(bool stayAboveSettingsDialog = false)
        {
            if (_opened)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
            }

            Topmost = stayAboveSettingsDialog;
            if (!IsVisible)
            {
                Show();
                _opened = true;
                Rebuild();
                if (_followTail)
                {
                    ScrollToEnd();
                }
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        }

        public void ClearStayAbove()
        {
            Topmost = false;
        }

        private static bool ReadLogDenoise()
        {
            return Config.Get("LogDenoise", true);
        }

        public void ApplyLogDenoiseSetting()
        {
            // The settings checkbox toggled: re-project now while the window is
            // open; a hidden window picks the setting up in its next Rebuild.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsVisible || ReadLogDenoise() == _filter.HideRepeats)
                {
                    return;
                }

                Rebuild();
                if (_followTail)
                {
                    ScrollToEnd();
                }
            }));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer(LogList);
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += OnScrollChanged;
            }

            Rebuild();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (_forceClose)
            {
                return;
            }

            e.Cancel = true;
            Hide();
        }

        private void OnAppExit(object sender, ExitEventArgs e)
        {
            _session.ActivityLogChanged -= OnActivityLogChanged;
            Application.Current.Exit -= OnAppExit;
            _forceClose = true;
        }

        private void OnActivityLogChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsVisible)
                {
                    return;
                }

                SyncRows();
            }));
        }

        private void Rebuild()
        {
            _filter = new ActivityLogRowFilter(ReadLogDenoise());
            _rows.Clear();
            _rowSources.Clear();
            _anchorIndex = -1;
            SyncRows();
        }

        private void SyncRows()
        {
            foreach (ActivityLogRow row in _filter.Consume(_session.ActivityLog))
            {
                _rows.Add(Project(row));
                _rowSources.Add(row);
            }

            for (int i = 0; i < _rowSources.Count; i++)
            {
                ApplyProjection(_rows[i], _rowSources[i]);
            }

            EmptyState.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private ActivityLogRowView Project(ActivityLogRow row)
        {
            var view = new ActivityLogRowView();
            ApplyProjection(view, row);
            return view;
        }

        private void ApplyProjection(ActivityLogRowView view, ActivityLogRow row)
        {
            view.Time = row.UtcTimestamp.ToLocalTime().ToString("HH:mm:ss");
            view.RegionPair = ResolveRegionPair(row);
            view.Job = ResolveJobs(row);
            view.Result = ResolveResult(row);
            view.IsRepeat = row.IsRepeat;
        }

        private string ResolveRegionPair(ActivityLogRow row)
        {
            switch (row.Scope)
            {
                case ActivityLogScope.DarkScreen:
                    return ResolveText("ActivityLog_Scope_DarkScreen", null);
                case ActivityLogScope.DialogueOptions:
                    return ResolveText("ActivityLog_Scope_DialogueOptions", null);
                case ActivityLogScope.Pair:
                    if (row.PairOrdinal.HasValue)
                    {
                        string key = row.VoicePrimary
                            ? "ActivityLog_Scope_VoicePrimary"
                            : "ActivityLog_Scope_Pair";
                        return ResolveText(key, new object[] { row.PairOrdinal.Value });
                    }

                    return ResolveText("ActivityLog_Scope_Global", null);
                default:
                    return ResolveText("ActivityLog_Scope_Global", null);
            }
        }

        private string ResolveJobs(ActivityLogRow row)
        {
            System.Collections.Generic.IReadOnlyList<OperatorJob> jobs = row.Jobs;
            if (jobs == null || jobs.Count == 0)
            {
                return ResolveText(JobResourceKey(row.Job), null);
            }

            string separator = ResolveText("ActivityLog_JobSeparator", null);
            if (string.IsNullOrEmpty(separator))
            {
                separator = " · ";
            }

            var parts = new string[jobs.Count];
            for (int i = 0; i < jobs.Count; i++)
            {
                parts[i] = ResolveText(JobResourceKey(jobs[i]), null);
            }

            string joined = string.Join(separator, parts);
            if (row.IsRepeat)
            {
                string repeatBadge = ResolveText("ActivityLog_RepeatBadge", null);
                if (!string.IsNullOrEmpty(repeatBadge))
                {
                    joined += separator + repeatBadge;
                }
            }

            return joined;
        }

        private string ResolveResult(ActivityLogRow row)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (row.DetectionMiss)
            {
                lines.Add(ResolveText("ActivityLog_Result_DetectionMiss", null));
            }
            else if (!string.IsNullOrEmpty(row.OcrText))
            {
                lines.Add(ResolveText("ActivityLog_Result_OcrText", new object[] { row.OcrText }));
            }
            else
            {
                string action = ResolveText(row.ResultResourceKey, row.ResultFormatArguments);
                if (!string.IsNullOrEmpty(action))
                {
                    lines.Add(action);
                }
            }

            if (row.MatchMiss)
            {
                lines.Add(ResolveText("ActivityLog_Result_MatchMiss", null));
            }
            else
            {
                if (!string.IsNullOrEmpty(row.Original))
                {
                    lines.Add(ResolveText("ActivityLog_Result_Original", new object[] { row.Original }));
                }

                if (!string.IsNullOrEmpty(row.Translation))
                {
                    lines.Add(ResolveText("ActivityLog_Result_Translation", new object[] { row.Translation }));
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string JobResourceKey(OperatorJob job)
        {
            switch (job)
            {
                case OperatorJob.StartRecognition:
                    return "ActivityLog_Job_StartRecognition";
                case OperatorJob.StopRecognition:
                    return "ActivityLog_Job_StopRecognition";
                case OperatorJob.HideSubtitles:
                    return "ActivityLog_Job_HideSubtitles";
                case OperatorJob.ShowSubtitles:
                    return "ActivityLog_Job_ShowSubtitles";
                case OperatorJob.BoxCapture:
                    return "ActivityLog_Job_BoxCapture";
                case OperatorJob.Refresh:
                    return "ActivityLog_Job_Refresh";
                case OperatorJob.VoiceSpeed:
                    return "ActivityLog_Job_VoiceSpeed";
                case OperatorJob.Preview:
                    return "ActivityLog_Job_Preview";
                case OperatorJob.Capture:
                    return "ActivityLog_Job_Capture";
                case OperatorJob.Ocr:
                    return "ActivityLog_Job_Ocr";
                case OperatorJob.Match:
                    return "ActivityLog_Job_Match";
                case OperatorJob.Voice:
                    return "ActivityLog_Job_Voice";
                case OperatorJob.LanguagePackLoad:
                    return "ActivityLog_Job_LanguagePackLoad";
                case OperatorJob.LanguagePackDownload:
                    return "ActivityLog_Job_LanguagePackDownload";
                default:
                    return string.Empty;
            }
        }

        private string ResolveText(string resourceKey, object[] formatArguments)
        {
            if (string.IsNullOrEmpty(resourceKey))
            {
                return string.Empty;
            }

            string format = TryFindResource(resourceKey) as string;
            if (string.IsNullOrEmpty(format))
            {
                return string.Empty;
            }

            if (formatArguments == null || formatArguments.Length == 0)
            {
                return format;
            }

            try
            {
                return string.Format(format, formatArguments);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_scrollViewer == null)
            {
                return;
            }

            bool atBottom = _scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - 1.0;
            if (e.ExtentHeightChange == 0)
            {
                _followTail = atBottom;
                if (_followTail)
                {
                    NewRecordsButton.Visibility = Visibility.Collapsed;
                }

                return;
            }

            if (_followTail)
            {
                ScrollToEnd();
                NewRecordsButton.Visibility = Visibility.Collapsed;
                return;
            }

            if (e.ExtentHeightChange > 0)
            {
                NewRecordsButton.Visibility = Visibility.Visible;
            }
        }

        private void NewRecordsButton_Click(object sender, RoutedEventArgs e)
        {
            _followTail = true;
            NewRecordsButton.Visibility = Visibility.Collapsed;
            ScrollToEnd();
        }

        private void ScrollToEnd()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollToEnd();
                return;
            }

            if (_rows.Count > 0)
            {
                LogList.ScrollIntoView(_rows[_rows.Count - 1]);
            }
        }

        private static ScrollViewer FindScrollViewer(DependencyObject root)
        {
            if (root == null)
            {
                return null;
            }

            if (root is ScrollViewer viewer)
            {
                return viewer;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                ScrollViewer child = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        // The cell TextBox consumes the bubbling mouse-down, so ListView row
        // selection has to run in the tunneling preview phase instead — but
        // only for clicks that land on a cell; anywhere else the native
        // selection handling stays in charge and must not apply twice.
        private void RowItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) == null)
            {
                return;
            }

            var item = (ListViewItem)sender;
            int index = LogList.Items.IndexOf(item.Content);
            if (index < 0)
            {
                return;
            }

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None;
            if (shift && _anchorIndex >= 0 && _anchorIndex < LogList.Items.Count)
            {
                if (!ctrl)
                {
                    LogList.UnselectAll();
                }

                int first = Math.Min(_anchorIndex, index);
                int last = Math.Max(_anchorIndex, index);
                for (int i = first; i <= last; i++)
                {
                    object row = LogList.Items[i];
                    if (!LogList.SelectedItems.Contains(row))
                    {
                        LogList.SelectedItems.Add(row);
                    }
                }

                return;
            }

            if (ctrl)
            {
                item.IsSelected = !item.IsSelected;
            }
            else
            {
                LogList.UnselectAll();
                item.IsSelected = true;
            }

            _anchorIndex = index;
        }

        private void LogList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TextBox cell = FindAncestor<TextBox>(e.OriginalSource as DependencyObject);
            if (cell != null && cell.SelectionLength > 0)
            {
                SetClipboardWithRetry(cell.SelectedText);
            }
        }

        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.C || (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
            {
                return;
            }

            if (GetCellSelection(null) != null)
            {
                return; // the focused cell copies its own selection natively
            }

            if (CopySelectedRowsToClipboard())
            {
                e.Handled = true;
            }
        }

        private void CopyMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            MenuItem copyItem = menu.Items.OfType<MenuItem>().FirstOrDefault();
            if (copyItem != null)
            {
                copyItem.IsEnabled = GetCellSelection(menu) != null || LogList.SelectedItems.Count > 0;
            }
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menu = ((MenuItem)sender).Parent as ContextMenu;
            string selection = GetCellSelection(menu);
            if (selection != null)
            {
                SetClipboardWithRetry(selection);
                return;
            }

            CopySelectedRowsToClipboard();
        }

        // While a context menu is open, keyboard focus sits on the menu, so
        // the right-clicked cell has to come from the placement target.
        private static string GetCellSelection(ContextMenu menu)
        {
            TextBox cell = null;
            if (menu != null)
            {
                cell = menu.PlacementTarget as TextBox;
            }

            if (cell == null)
            {
                cell = Keyboard.FocusedElement as TextBox;
            }

            if (cell != null && cell.SelectionLength > 0)
            {
                return cell.SelectedText;
            }

            return null;
        }

        private bool CopySelectedRowsToClipboard()
        {
            if (LogList.SelectedItems.Count == 0)
            {
                return false;
            }

            var lines = new List<string>();
            foreach (ActivityLogRowView row in _rows)
            {
                if (LogList.SelectedItems.Contains(row))
                {
                    lines.Add(row.ToTsv());
                }
            }

            SetClipboardWithRetry(string.Join(Environment.NewLine, lines));
            return true;
        }

        private static void SetClipboardWithRetry(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return;
                }
                catch (COMException)
                {
                }
                catch (ExternalException)
                {
                }

                if (attempt >= 2)
                {
                    return;
                }

                System.Threading.Thread.Sleep(30);
            }
        }

        private static T FindAncestor<T>(DependencyObject element) where T : class
        {
            while (element != null && !(element is T))
            {
                var content = element as FrameworkContentElement;
                element = content != null
                    ? content.Parent
                    : VisualTreeHelper.GetParent(element);
            }

            return element as T;
        }
    }

    internal sealed class ActivityLogRowView : INotifyPropertyChanged
    {
        private string _time;
        private string _regionPair;
        private string _job;
        private string _result;
        private bool _isRepeat;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Time
        {
            get { return _time; }
            set { SetField(ref _time, value, nameof(Time)); }
        }

        public string RegionPair
        {
            get { return _regionPair; }
            set { SetField(ref _regionPair, value, nameof(RegionPair)); }
        }

        public string Job
        {
            get { return _job; }
            set { SetField(ref _job, value, nameof(Job)); }
        }

        public string Result
        {
            get { return _result; }
            set { SetField(ref _result, value, nameof(Result)); }
        }

        public bool IsRepeat
        {
            get { return _isRepeat; }
            set { SetField(ref _isRepeat, value, nameof(IsRepeat)); }
        }

        public string ToTsv()
        {
            return Time + "\t" + RegionPair + "\t" + Job + "\t" + Result;
        }

        private void SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
