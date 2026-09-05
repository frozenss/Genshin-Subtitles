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

            int n = Math.Min(_projectedCount, _session.ActivityLog.Count);
            for (int i = 0; i < n; i++)
            {
                ApplyProjection(_rows[i], _session.ActivityLog[i]);
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

            return string.Join(separator, parts);
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
    }

    internal sealed class ActivityLogRowView : INotifyPropertyChanged
    {
        private string _time;
        private string _regionPair;
        private string _job;
        private string _result;

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

        private void SetField(ref string field, string value, string propertyName)
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
