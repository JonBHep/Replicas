using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Replicas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
            public MainWindow()
        {
            InitializeComponent();
            _availableList = new ObservableCollection<JobListItem>();
            _unavailableList = new ObservableCollection<JobListItem>();
            _tmrUpdate.Tick += UpdateTimer_Tick;
        }

        private readonly System.Windows.Threading.DispatcherTimer _tmrUpdate = new System.Windows.Threading.DispatcherTimer();

        private class JobListItem
        {
            public string? Description { get; set; }
            public string? Priority { get; set; }
            public string? LastRun { get; set; }
            public string? WhichWas { get; set; }
            public string? Urgency { get; set; }
            public SolidColorBrush? UrgencyColour { get; set; }
            public SolidColorBrush? DangerColour { get; set; }
            public SolidColorBrush? OldestFlashColour { get; set; }
            public string? Tag { get; set; }
        }

        private ObservableCollection<JobListItem> _availableList;
        private ObservableCollection<JobListItem> _unavailableList;
        private enum ListMode { All, Info, Business, Other};
        private ListMode _lmode = ListMode.All;

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            RefreshElapsedTimes();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _tmrUpdate.Stop();
            Kernel.Instance.ShutDown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            double scrX = SystemParameters.PrimaryScreenWidth;
            double scrY = SystemParameters.PrimaryScreenHeight;
            double winX = scrX * .98;
            double winY = scrY * .94;
            double xm = (scrX - winX) / 2;
            double ym = (scrY - winY) / 4;
            Width = winX; Height = winY;
            Left = xm;
            Top = ym;

            Title = "Replicate";
            RunButtons(false);
            BtnDelete.IsEnabled = false;
            BtnAdd.IsEnabled = false;
            BtnRefresh.IsEnabled = false;
            BtnClose.IsEnabled = false;
        }

        private static bool JobOk(Tache t)
        {
            bool b = true;
            if (string.IsNullOrWhiteSpace(t.SourceFoundDrivePath()))
            { b = false; }
            else if (string.IsNullOrWhiteSpace(t.DestinationFoundDrivePath()))
            { b = false; }
            return b;
        }

        private List<Tache> ThinnedList()
        {
            List<Tache> sortinglist = Kernel.Instance.JobProfiles.Jobs.Values.ToList();
            sortinglist.Sort();
            switch (_lmode)
            {
                case ListMode.Info:
                    {
                        List<Tache> slim = new List<Tache>();
                        foreach (Tache t in sortinglist)
                        {
                            if (t.IsJbhInfoBackup) { slim.Add(t); }
                        }
                        return slim;
                    }
                case ListMode.Business:
                    {
                        List<Tache> slim = new List<Tache>();
                        foreach (Tache t in sortinglist)
                        {
                            if (t.IsJbhBusinessBackup) { slim.Add(t); }
                        }
                        return slim;
                    }
                case ListMode.Other:
                    {
                        List<Tache> slim = new List<Tache>();
                        foreach (Tache t in sortinglist)
                        {
                            if (!t.IsJbhSpecialBackup) { slim.Add(t); }
                        }
                        return slim;
                    }
                default: { return sortinglist; }
            }
        }
        private void RefreshLists()
        {
            List<Tache> appropriatelist = ThinnedList();
            InstructionTextBlock.Text = string.Empty;
            _availableList.Clear();
            _unavailableList.Clear();
            Kernel.Instance.JobProfiles.AllocateJbhInfoFreshnessRanking();
            Kernel.Instance.JobProfiles.AllocateJbhBusinessFreshnessRanking();

            bool overdue= false;
            if ((_lmode == ListMode.Info) || (_lmode == ListMode.Business))
            {
                foreach (Tache t in appropriatelist)
                {
                    if (t.Newest)
                    {
                        if (t.DuePercent() > 100)
                        {
                            overdue = true;
                            InstructionTextBlock.Text = "As the newest backup is not fresh, perform the backup marked 'Oldest'"; 
                            InstructionTextBlock.Foreground = Brushes.Red;
                        }
                        else
                        {
                            InstructionTextBlock.Text = "The newest backup is acceptably recent"; 
                            InstructionTextBlock.Foreground = Brushes.Gray;
                        }
                    }
                }
            }
            else
            {
                InstructionTextBlock.Text = string.Empty;
            }

            foreach (Tache t in appropriatelist)
            {
                JobListItem jli = new JobListItem()
                {
                    Tag = t.Key,
                    Description = t.JobTitle,
                    Priority = t.Oldest ? "Oldest" : t.Newest ? "Newest" : string.Empty,
                    OldestFlashColour = t.Oldest ? overdue ? Brushes.Yellow : Brushes.Transparent : Brushes.Transparent
                };

                TimeSpan ago = DateTime.Now - t.LastDate;
                TimeSpan nineMth = new TimeSpan(274, 0, 0, 0);
                TimeSpan oneMth = new TimeSpan(30, 0, 0, 0);

                if (t.LastDate < new DateTime(2010, 1, 1))
                { jli.LastRun = "Never"; }
                else if (ago < oneMth)
                {
                    jli.LastRun = t.LastDate.ToString("dd MMM - HH:mm", CultureInfo.CurrentCulture);
                }
                else if (ago < nineMth)
                {
                    jli.LastRun = t.LastDate.ToString("dd MMM", CultureInfo.CurrentCulture);
                }
                else
                { jli.LastRun = t.LastDate.ToString("dd MMM yyyy", CultureInfo.CurrentCulture); }

                jli.WhichWas = t.PeriodSinceLastRun();

                int u = t.DuePercent();
                jli.Urgency = (u < 0) ? string.Empty : $"{u}%";
                jli.UrgencyColour = t.DueColorBrush();
                jli.DangerColour = t.Dangerous ? Brushes.Red : Brushes.SaddleBrown;
                // which listview to add the job to
                if (JobOk(t)) { _availableList.Add(jli); } else { _unavailableList.Add(jli); }
            }

            BtnEdit.IsEnabled = false;
            BtnDelete.IsEnabled = false;
            LblJobTitle.Text = string.Empty;
            LblJobSourcePath.Text = string.Empty;
            LblJobSourceVolume.Text = string.Empty;
            LblJobDestinationPath.Text = string.Empty;
            LblJobDestinationVolume.Text = string.Empty;
            LblJobFeatures.Text = string.Empty;
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow w = new AboutWindow()
            {
                Owner = this
            };
            w.ShowDialog();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Kernel.Instance.ForceRefresh();
            RefreshLists();
        }

        private void RefreshElapsedTimes()
        {
            // updates display in lists of time since last backup
            foreach (JobListItem jli in _availableList)
            {
                if (jli.Tag is not { } quoi) continue;
                jli.WhichWas = Kernel.Instance.JobProfiles.Jobs[quoi].PeriodSinceLastRun();
            }

            foreach (JobListItem jli in _unavailableList)
            {
                if (jli.Tag is not { } quoi) continue;
                jli.WhichWas = Kernel.Instance.JobProfiles.Jobs[quoi].PeriodSinceLastRun();
            }
        }

        private static string KbReport(long byteCount)
        {
            string rv = byteCount.ToString(CultureInfo.CurrentCulture) + " bytes";
            if (byteCount > 1000) { rv = (byteCount / 1000).ToString("0 KB", CultureInfo.CurrentCulture); }
            if (byteCount > 1000000) { rv = (byteCount / 1000000).ToString("0 MB", CultureInfo.CurrentCulture); }
            if (byteCount > 1000000000) { rv = (byteCount / 1000000000).ToString("0 GB", CultureInfo.CurrentCulture); }
            return rv;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lvw)
            {
                ListView otherListView = (lvw == LvwAvailable) ? LvwUnavailable: LvwAvailable;
                otherListView.SelectedIndex = -1;

                if (lvw.SelectedItem == null)
                { // no job is selected
                    Kernel.Instance.CurrentTask =string.Empty;
                    LblJobTitle.Text = string.Empty;
                    LblJobSourcePath.Text = string.Empty;
                    LblJobSourceVolume.Text = string.Empty;
                    LblJobDestinationPath.Text = string.Empty;
                    LblJobDestinationVolume.Text = string.Empty;
                    LblJobFeatures.Text = string.Empty;
                    RunButtons(false);
                    BtnDelete.IsEnabled = false;
                    BtnEdit.IsEnabled = false;
                }
                else
                { // a job is selected
                    if (lvw.SelectedItem is JobListItem job)
                    {
                        if (job.Tag is {} tog)
                        {
                            Kernel.Instance.CurrentTask = tog;

                            LblJobSourcePath.Foreground = Brushes.Purple;
                            LblJobSourceVolume.Foreground = Brushes.Purple;
                            LblJobDestinationPath.Foreground = Brushes.Purple;
                            LblJobDestinationVolume.Foreground = Brushes.Purple;

                            Tache boulot = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask];
                            LblJobTitle.Text = boulot.JobTitle;
                            LblJobSourcePath.Text = boulot.SourcePath;
                            LblJobDestinationPath.Text = boulot.DestinationPath;
                            LblJobSourceVolume.Text = Kernel.Instance.KnownDrives.VolumeDescription(boulot.SourceVolume) + " (" + boulot.SourceVolume + ")";

                            long tot = boulot.DestinationSize(); 
                            long fre = boulot.DestinationFree();

                            if (tot < 0)
                            {
                                LblJobDestinationVolume.Text
                                    = Kernel.Instance.KnownDrives.VolumeDescription(boulot.DestinationVolume) + " (" +
                                      boulot.DestinationVolume + ")";
                            }
                            else
                            {
                                LblJobDestinationVolume.Text
                                    = Kernel.Instance.KnownDrives.VolumeDescription(boulot.DestinationVolume) + " (" +
                                      boulot.DestinationVolume + ") " + KbReport(tot) + " total " + KbReport(fre) +
                                      " free (" +
                                      (Convert.ToInt32(100 * ((double) fre / tot)))
                                      .ToString(CultureInfo.CurrentCulture) + "% free)";
                            }

                            string features;
                            features = boulot.IncludeHidden ? "Hidden files included, " : "Hidden files excluded, ";

                            features += boulot.IsJbhInfoBackup ? "backs up Jbh.Info." : "does not back up Jbh.Info.";

                            features += boulot.IsJbhBusinessBackup
                                ? "backs up Jbh.Business."
                                : "does not back up Jbh.Business.";

                            LblJobFeatures.Text = features;

                            RunButtons(true);
                            BtnEdit.IsEnabled = true;

                            if (string.IsNullOrWhiteSpace(boulot.SourceFoundDrivePath()))
                            { // source not accesible
                                LblJobSourcePath.Foreground = Brushes.Red;
                                RunButtons(false);
                            }

                            if (string.IsNullOrWhiteSpace(boulot.DestinationFoundDrivePath()))
                            { // destination not accesible
                                LblJobDestinationPath.Foreground = Brushes.Red;
                                RunButtons(false);
                            }

                            // signal if drive not found (as well as full path)
                            if (Kernel.Instance.DrivesCurrentlyFound.DriveLetter(boulot.SourceVolume) == null) { LblJobSourceVolume.Foreground = Brushes.Red; }
                            if (Kernel.Instance.DrivesCurrentlyFound.DriveLetter(boulot.DestinationVolume) == null) { LblJobDestinationVolume.Foreground = Brushes.Red; }
                            BtnDelete.IsEnabled = true;
                        }
                    }
                }
            }
        } 

        private void SelectTargetItem(string target)
        {
            int itm = -1;
            ListView lvw = LvwAvailable;
            for (int x = 0; x < lvw.Items.Count; x++)
            {
                if (lvw.Items[x] is JobListItem jobLi)
                {
                    if (jobLi.Tag is { } aString)
                    {
                        if (aString == target)
                        {
                            itm = x;
                        }
                    }    
                }
            }
            if (itm == -1)
            {
                lvw = LvwUnavailable;
                for (int x = 0; x < lvw.Items.Count; x++)
                {
                    if (lvw.Items[x] is JobListItem jobLi)
                    {
                        if (jobLi.Tag is { } aString)
                        {
                            if (aString == target)
                            {
                                itm = x;
                            }
                        }    
                    }
                }
            }
            if (itm >= 0)
            {
                lvw.SelectedIndex = itm;
                lvw.Focus();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditItem();
        }
        
        private void EditItem()
        {
            if (!string.IsNullOrWhiteSpace(Kernel.Instance.CurrentTask))
            {
                JobEditorWindow w = new JobEditorWindow();

                Tache t = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask];
                w.AaTitle = t.JobTitle;
                w.AaIsJbhInfoBackup = t.IsJbhInfoBackup;
                w.AaIsJbhBusinessBackup = t.IsJbhBusinessBackup;
                w.AaDestinationPath = t.DestinationPath;
                w.AaDestinationVolume = t.DestinationVolume;
                w.AaIncludeHiddenFiles = t.IncludeHidden;
                w.AaDangerous = t.Dangerous;
                w.AaSourcePath = t.SourcePath;
                w.AaSourceVolume = t.SourceVolume;
                if (t.PathsInaccessible()) { w.Inaccessible = true; }
                if (w.ShowDialog() == true)
                {
                    Kernel.Instance.JobProfiles.UpdateJob(Kernel.Instance.CurrentTask, w.AaTitle, w.AaSourcePath, w.AaSourceVolume, w.AaDestinationPath, w.AaDestinationVolume, w.AaIncludeHiddenFiles, w.AaDangerous, w.AaIsJbhInfoBackup, w.AaIsJbhBusinessBackup);
                }
                RefreshLists();
                SelectTargetItem(Kernel.Instance.CurrentTask);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Kernel.Instance.ForceRefresh(); // refresh so as to identify any recently plugged-in drives
            RefreshLists();

            JobEditorWindow w = new JobEditorWindow()
            {
                AaTitle = "?"
            };
            if (w.ShowDialog() == false) { return; }

            string nookey = Kernel.Instance.JobProfiles.AddJob(jTitle: w.AaTitle, sPath: w.AaSourcePath, sVolume: w.AaSourceVolume, dPath: w.AaDestinationPath, dVolume: w.AaDestinationVolume, dHidden: w.AaIncludeHiddenFiles, dDangerous: w.AaDangerous, dJbhInfo: w.AaIsJbhInfoBackup, dJbhBusiness: w.AaIsJbhBusinessBackup);
            RefreshLists();
            SelectTargetItem(nookey);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            string msg = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask].JobTitle;
            MessageBoxResult ans = MessageBox.Show("Delete the selected job?\n\n" + msg, "Delete backup job", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ans == MessageBoxResult.No) { return; }
            Kernel.Instance.JobProfiles.Jobs.Remove(Kernel.Instance.CurrentTask);
            RefreshLists();
        }

        private void DrivesButton_Click(object sender, RoutedEventArgs e)
        {
            DrivesWindow w = new DrivesWindow() { Owner = this };
            w.ShowDialog();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            Runner();
        }

        private void Runner()
        {
            if (Kernel.Instance.CurrentTask is null){return;}
            if (!Kernel.Instance.JobProfiles.Jobs.ContainsKey(Kernel.Instance.CurrentTask)){return;}
            
            Tache one = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask];
            if (one.Dangerous)
            {
                MessageBoxResult challenge
                    = MessageBox.Show("This job is marked as dangerous. It risks overwriting good data.\n\nGo ahead?"
                        , Jbh.AppManager.AppName, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                if (challenge == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            _tmrUpdate.IsEnabled = false;
            RunWindow w = new RunWindow(one) {Owner = this};
            Hide();
            w.ShowDialog();
            Show();
            if (w.Fulfilled)
            {
                one.LastDate = DateTime.Now;
            }
            else
            {
                MessageBox.Show("The backup job was not fully completed", "Replicas", MessageBoxButton.OK
                    , MessageBoxImage.Asterisk);
            }

            _tmrUpdate.IsEnabled = true;
            Kernel.Instance.JobProfiles
                .SaveProfile(); // To ensure this backup event is recorded before the user has the opportunity to click the Refresh button
            RunButtons(false);
            BtnEdit.IsEnabled = false;
            RefreshLists();
            UpdateFreshness();
        }

        private void RunButtons(bool flick)
        {
            BtnRun.IsEnabled = BtnRun2.IsEnabled = flick;
        }
        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditItem();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Jbh.AppManager.DataPath)) { MessageBox.Show("Path to data is not found.\n\nKeepers will close.", "Databank is not accessible", MessageBoxButton.OK, MessageBoxImage.Error); Close(); }

            _availableList = new ObservableCollection<JobListItem>();
            _unavailableList = new ObservableCollection<JobListItem>();
            RefreshLists();
            BtnRefresh.IsEnabled = true;
            LblJobTitle.Text = string.Empty;
            LblJobSourcePath.Text = string.Empty;
            LblJobSourceVolume.Text = string.Empty;
            LblJobDestinationPath.Text = string.Empty;
            LblJobFeatures.Text = string.Empty;
            BtnAdd.IsEnabled = true;
            BtnClose.IsEnabled = true;
            _tmrUpdate.Interval = new TimeSpan(days: 0, hours: 0, minutes: 1, seconds: 0, milliseconds: 0);
            _tmrUpdate.IsEnabled = true;

            LvwAvailable.ItemsSource = _availableList;
            LvwUnavailable.ItemsSource = _unavailableList;
            UpdateFreshness();
        }

        private void UpdateFreshness()
        {
            List<string> infoJobList = Kernel.Instance.JobProfiles.JbhInfoDates();
            List<string> busiJobList = Kernel.Instance.JobProfiles.JbhBusinessDates();
            string qi = infoJobList.First();
            string zi = infoJobList.Last();
            string qb = busiJobList.First();
            string zb = busiJobList.Last();
            string[] pi = qi.Split("^".ToCharArray());
            string[] pb = qb.Split("^".ToCharArray());
            string[] vi = zi.Split("^".ToCharArray());
            string[] vb = zb.Split("^".ToCharArray());
            InfoBlocB.Text = vi[1];
            InfoBlocA.Text = pi[2];
            BusinessBlocB.Text = vb[1];
            BusinessBlocA.Text = pb[2];

            List<Tuple<int, int>> jobcounts = Kernel.Instance.JobProfiles.JobCounts();
            BusinessBlocC.Text = $"{jobcounts[0].Item1 + jobcounts[0].Item2}";
            BusinessBlocD.Text = $"{jobcounts[0].Item1}";
            BusinessBlocE.Text = $"{jobcounts[0].Item2}";

            InfoBlocC.Text = $"{jobcounts[1].Item1 + jobcounts[1].Item2}";
            InfoBlocD.Text = $"{jobcounts[1].Item1}";
            InfoBlocE.Text = $"{jobcounts[1].Item2}";

            OtherBlocC.Text = $"{jobcounts[2].Item1 + jobcounts[2].Item2}";
            OtherBlocD.Text = $"{jobcounts[2].Item1}";
            OtherBlocE.Text = $"{jobcounts[2].Item2}";
        }

        private void DatabankButton_Click(object sender, RoutedEventArgs e)
        {
            JbhDataReportWindow w = new JbhDataReportWindow(Kernel.Instance.JobProfiles.JbhInfoDates(), Kernel.Instance.JobProfiles.JbhBusinessDates())
            {
                Owner = this
            };
            w.ShowDialog();
        }
        
        private void FamilyMatButton_Click(object sender, RoutedEventArgs e)
        {
            FamilyPublishWindow w = new FamilyPublishWindow(false) {Owner = this};
                w.ShowDialog();
        }

        private void FamilyPatButton_Click(object sender, RoutedEventArgs e)
        {
            FamilyPublishWindow w = new FamilyPublishWindow(true) {Owner = this};
            
                w.ShowDialog();
        }

        private void ListInfoButton_Click(object sender, RoutedEventArgs e)
        {
            _lmode = ListMode.Info;
            SelectionTextBlock.Text = "Jbh.Info backup jobs";
            RefreshLists();
        }
        private void ListBusinesButton_Click(object sender, RoutedEventArgs e)
        {
            _lmode = ListMode.Business;
            SelectionTextBlock.Text = "Jbh.Business backup jobs";
            RefreshLists();
        }
        private void ListOtherButton_Click(object sender, RoutedEventArgs e)
        {
            _lmode = ListMode.Other;
            SelectionTextBlock.Text = "Miscellaneous jobs";
            RefreshLists();
        }
    }
}