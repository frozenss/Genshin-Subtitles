using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GI_Subtitles.Core.Overlay;

namespace GI_Subtitles.Views
{
    public partial class ActivityLogWindow : Window
    {
        private readonly LiveOverlaySession _session;
        private readonly ObservableCollection<ActivityLogRowView> _rows = new ObservableCollection<ActivityLogRowView>();
        private ScrollViewer _scrollViewer;
        private bool _followTail = true;
        private bool _forceClose;
        private bool _opened;
        private int _projectedCount;

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
            _rows.Clear();
            _projectedCount = 0;
            SyncRows();
        }

        private void SyncRows()
        {
            while (_projectedCount < _session.ActivityLog.Count)
            {
                _rows.Add(Project(_session.ActivityLog[_projectedCount]));
                _projectedCount++;
            }

            EmptyState.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private ActivityLogRowView Project(ActivityLogRow row)
        {
            return new ActivityLogRowView
            {
                Time = row.UtcTimestamp.ToLocalTime().ToString("HH:mm:ss"),
                RegionPair = ResolveRegionPair(row.PairOrdinal),
                Job = ResolveText(JobResourceKey(row.Job), null),
                Result = ResolveText(row.ResultResourceKey, row.ResultFormatArguments)
            };
        }

        private string ResolveRegionPair(int? pairOrdinal)
        {
            if (!pairOrdinal.HasValue)
            {
                return ResolveText("ActivityLog_Scope_Global", null);
            }

            return ResolveText("Overlay_PairOutlineLabel", new object[] { pairOrdinal.Value });
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
    }

    internal sealed class ActivityLogRowView
    {
        public string Time { get; set; }

        public string RegionPair { get; set; }

        public string Job { get; set; }

        public string Result { get; set; }
    }
}
