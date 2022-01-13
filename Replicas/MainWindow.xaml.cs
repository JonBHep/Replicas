using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Replicas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
            public MainWindow()
        {
            InitializeComponent();
            _tmrUpdate.Tick += new EventHandler(UpdateTimer_Tick);
        }

        private readonly System.Windows.Threading.DispatcherTimer _tmrUpdate = new System.Windows.Threading.DispatcherTimer();

        private class JobListItem
        {
            public string Description { get; set; }
            public string Priority { get; set; }
            public string Lastrun { get; set; }
            public string Whichwas { get; set; }
            public string Urgency { get; set; }
            public SolidColorBrush Urgencycolour { get; set; }
            public SolidColorBrush Dangercolour { get; set; }
            public SolidColorBrush OldestFlashColour { get; set; }
            public string Tag { get; set; }
        }

        private System.Collections.ObjectModel.ObservableCollection<JobListItem> _availableList;
        private System.Collections.ObjectModel.ObservableCollection<JobListItem> _unavailableList;
        private enum ListMode { All, Info, Business, Other};
        private ListMode lmode = ListMode.All;

        private void UpdateTimer_Tick(object sender, EventArgs e)
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
            double Xm = (scrX - winX) / 2;
            double Ym = (scrY - winY) / 4;
            Width = winX; Height = winY;
            Left = Xm;
            Top = Ym;

            Title = "Replicate";
            RunButtons(false);
            btnDelete.IsEnabled = false;
            btnAdd.IsEnabled = false;
            btnRefresh.IsEnabled = false;
            btnClose.IsEnabled = false;
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
            switch (lmode)
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
            if ((lmode == ListMode.Info) || (lmode == ListMode.Business))
            {
                foreach (Tache t in appropriatelist)
                {
                    if (t.Newest)
                    {
                        if (t.Dueness() > 100)
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
                TimeSpan NineMth = new TimeSpan(274, 0, 0, 0);
                TimeSpan OneMth = new TimeSpan(30, 0, 0, 0);

                if (t.LastDate < new DateTime(2010, 1, 1))
                { jli.Lastrun = "Never"; }
                else if (ago < OneMth)
                {
                    jli.Lastrun = t.LastDate.ToString("dd MMM - HH:mm", CultureInfo.CurrentCulture);
                }
                else if (ago < NineMth)
                {
                    jli.Lastrun = t.LastDate.ToString("dd MMM", CultureInfo.CurrentCulture);
                }
                else
                { jli.Lastrun = t.LastDate.ToString("dd MMM yyyy", CultureInfo.CurrentCulture); }

                jli.Whichwas = t.PeriodSinceLastRun();

                int u = t.Dueness();
                jli.Urgency = (u < 0) ? string.Empty : $"{u}%";
                //if ((lmode == ListMode.Info) || (lmode == ListMode.Business))
                //{
                //    if (t.Newest)
                //    {
                //        if (u > 100)
                //        { InstructionTextBlock.Text = "As the newest backup is not fresh, perform the backup marked 'Oldest'"; InstructionTextBlock.Foreground = Brushes.Red; }
                //        else
                //        { InstructionTextBlock.Text = "The newest backup is acceptably recent"; InstructionTextBlock.Foreground = Brushes.Gray; }
                //    }
                //}
                //else
                //{
                //    InstructionTextBlock.Text = string.Empty;
                //}

                jli.Urgencycolour = t.DuenessColorBrush();
                jli.Dangercolour = t.Dangerous ? Brushes.Red : Brushes.SaddleBrown;
                // which listview to add the job to
                if (JobOk(t)) { _availableList.Add(jli); } else { _unavailableList.Add(jli); }
            }

            btnEdit.IsEnabled = false;
            btnDelete.IsEnabled = false;
            lblJobTitle.Text = string.Empty;
            lblJobSourcePath.Text = string.Empty;
            lblJobSourceVolume.Text = string.Empty;
            lblJobDestinationPath.Text = string.Empty;
            lblJobDestinationVolume.Text = string.Empty;
            lblJobFeatures.Text = string.Empty;
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            Jbh.AboutWindow w = new Jbh.AboutWindow()
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
                string j = jli.Tag;
                jli.Whichwas = Kernel.Instance.JobProfiles.Jobs[j].PeriodSinceLastRun();
            }
            foreach (JobListItem jli in _unavailableList)
            {
                string j = jli.Tag;
                jli.Whichwas = Kernel.Instance.JobProfiles.Jobs[j].PeriodSinceLastRun();
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
                ListView otherListView = (lvw == lvwAvailable) ? lvwUnavailable: lvwAvailable;
                otherListView.SelectedIndex = -1;

                if (lvw.SelectedItem == null)
                { // no job is selected
                    Kernel.Instance.CurrentTask =string.Empty;
                    lblJobTitle.Text = string.Empty;
                    lblJobSourcePath.Text = string.Empty;
                    lblJobSourceVolume.Text = string.Empty;
                    lblJobDestinationPath.Text = string.Empty;
                    lblJobDestinationVolume.Text = string.Empty;
                    lblJobFeatures.Text = string.Empty;
                    RunButtons(false);
                    btnDelete.IsEnabled = false;
                    btnEdit.IsEnabled = false;
                }
                else
                { // a job is selected
                    if (lvw.SelectedItem is JobListItem job)
                    {
                        if (job.Tag is string tog)
                        {
                            Kernel.Instance.CurrentTask = tog;

                            lblJobSourcePath.Foreground = Brushes.Purple;
                            lblJobSourceVolume.Foreground = Brushes.Purple;
                            lblJobDestinationPath.Foreground = Brushes.Purple;
                            lblJobDestinationVolume.Foreground = Brushes.Purple;

                            Tache boulot = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask];
                            lblJobTitle.Text = boulot.JobTitle;
                            lblJobSourcePath.Text = boulot.SourcePath;
                            lblJobDestinationPath.Text = boulot.DestinationPath;
                            lblJobSourceVolume.Text = Kernel.Instance.KnownDrives.VolumeDescription(boulot.SourceVolume) + " (" + boulot.SourceVolume + ")";

                            long tot = boulot.DestinationSize(); ;
                            long fre = boulot.DestinationFree();

                            if (tot < 0)
                            { lblJobDestinationVolume.Text = Kernel.Instance.KnownDrives.VolumeDescription(boulot.DestinationVolume) + " (" + boulot.DestinationVolume + ")"; }
                            else
                            { lblJobDestinationVolume.Text = Kernel.Instance.KnownDrives.VolumeDescription(boulot.DestinationVolume) + " (" + boulot.DestinationVolume + ") " + KbReport(tot) + " total " + KbReport(fre) + " free (" + (Convert.ToInt32(100 * ((double)fre / tot))).ToString(CultureInfo.CurrentCulture) + "% free)"; }

                            string features;
                            if (boulot.IncludeHidden) { features = "Hidden files included, "; } else { features = "Hidden files excluded, "; }
                            if (boulot.IsJbhInfoBackup) { features += "backs up Jbh.Info."; } else { features += "does not back up Jbh.Info."; }
                            if (boulot.IsJbhBusinessBackup) { features += "backs up Jbh.Business."; } else { features += "does not back up Jbh.Business."; }

                            lblJobFeatures.Text = features;

                            RunButtons(true);
                            btnEdit.IsEnabled = true;

                            if (string.IsNullOrWhiteSpace(boulot.SourceFoundDrivePath()))
                            { // source not accesible
                                lblJobSourcePath.Foreground = Brushes.Red;
                                RunButtons(false);
                            }

                            if (string.IsNullOrWhiteSpace(boulot.DestinationFoundDrivePath()))
                            { // destination not accesible
                                lblJobDestinationPath.Foreground = Brushes.Red;
                                RunButtons(false);
                            }

                            // signal if drive not found (as well as full path)
                            if (Kernel.Instance.DrivesCurrentlyFound.DriveLetter(boulot.SourceVolume) == null) { lblJobSourceVolume.Foreground = Brushes.Red; }
                            if (Kernel.Instance.DrivesCurrentlyFound.DriveLetter(boulot.DestinationVolume) == null) { lblJobDestinationVolume.Foreground = Brushes.Red; }
                            btnDelete.IsEnabled = true;
                        }
                    }
                }
            }
        } 

        private void SelectTargetItem(string target)
        {
            int itm = -1;
            ListView lvw = lvwAvailable;
            for (int x = 0; x < lvw.Items.Count; x++)
            {
                JobListItem jli = lvw.Items[x] as JobListItem;
                if (jli.Tag == target) { itm = x; }
            }
            if (itm == -1)
            {
                lvw = lvwUnavailable;
                for (int x = 0; x < lvw.Items.Count; x++)
                {
                    JobListItem jli = lvw.Items[x] as JobListItem;
                    if (jli.Tag == target) { itm = x; }
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
            Tache one = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask];
            if (one.Dangerous)
            {
                MessageBoxResult challenge = MessageBox.Show("This job is marked as dangerous. It risks overwriting good data.\n\nGo ahead?", Jbh.AppManager.AppName, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                if (challenge == MessageBoxResult.Cancel) { return; }
            }
            using (RunWindow w = new RunWindow())
            {
                _tmrUpdate.IsEnabled = false;
                w.Owner = this;
                this.Hide();
                w.ShowDialog();
                this.Show();
                if (w.Fulfilled) {one.LastDate= DateTime.Now; } else { MessageBox.Show("The backup job was not fully completed", "Replicas", MessageBoxButton.OK, MessageBoxImage.Asterisk); }
            }
            _tmrUpdate.IsEnabled = true;
            Kernel.Instance.JobProfiles.SaveProfile(); // To ensure this backup event is recorded before the user has the opportunity to click the Refresh button
            RunButtons(false);
            btnEdit.IsEnabled = false;
            RefreshLists();
            UpdateFreshness();
        }

        private void RunButtons(bool flick)
        {
            btnRun.IsEnabled = btnRun2.IsEnabled = flick;
        }
        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditItem();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Jbh.AppManager.DataPath)) { MessageBox.Show("Path to data is not found.\n\nKeepers will close.", "Databank is not accessible", MessageBoxButton.OK, MessageBoxImage.Error); Close(); }

            _availableList = new System.Collections.ObjectModel.ObservableCollection<JobListItem>();
            _unavailableList = new System.Collections.ObjectModel.ObservableCollection<JobListItem>();
            RefreshLists();
            btnRefresh.IsEnabled = true;
            lblJobTitle.Text = string.Empty;
            lblJobSourcePath.Text = string.Empty;
            lblJobSourceVolume.Text = string.Empty;
            lblJobDestinationPath.Text = string.Empty;
            lblJobFeatures.Text = string.Empty;
            btnAdd.IsEnabled = true;
            btnClose.IsEnabled = true;
            _tmrUpdate.Interval = new TimeSpan(days: 0, hours: 0, minutes: 1, seconds: 0, milliseconds: 0);
            _tmrUpdate.IsEnabled = true;

            lvwAvailable.ItemsSource = _availableList;
            lvwUnavailable.ItemsSource = _unavailableList;
            UpdateFreshness();
        }

        private void UpdateFreshness()
        {
            List<string> InfoJobList = Kernel.Instance.JobProfiles.JbhInfoDates();
            List<string> BusiJobList = Kernel.Instance.JobProfiles.JbhBusinessDates();
            string qi = InfoJobList.First();
            string zi = InfoJobList.Last();
            string qb = BusiJobList.First();
            string zb = BusiJobList.Last();
            string[] pi = qi.Split("^".ToCharArray());
            string[] pb = qb.Split("^".ToCharArray());
            string[] vi = zi.Split("^".ToCharArray());
            string[] vb = zb.Split("^".ToCharArray());
            InfoBlocB.Text = vi[1];
            InfoBlocA.Text = pi[2];
            BusiBlocB.Text = vb[1];
            BusiBlocA.Text = pb[2];

            List<Tuple<int, int>> jobcounts = Kernel.Instance.JobProfiles.JobCounts();
            BusiBlocC.Text = $"{jobcounts[0].Item1 + jobcounts[0].Item2}";
            BusiBlocD.Text = $"{jobcounts[0].Item1}";
            BusiBlocE.Text = $"{jobcounts[0].Item2}";

            InfoBlocC.Text = $"{jobcounts[1].Item1 + jobcounts[1].Item2}";
            InfoBlocD.Text = $"{jobcounts[1].Item1}";
            InfoBlocE.Text = $"{jobcounts[1].Item2}";

            OthrBlocC.Text = $"{jobcounts[2].Item1 + jobcounts[2].Item2}";
            OthrBlocD.Text = $"{jobcounts[2].Item1}";
            OthrBlocE.Text = $"{jobcounts[2].Item2}";
        }

        private void DatabankButton_Click(object sender, RoutedEventArgs e)
        {
            JbhInfoReportWindow w = new JbhInfoReportWindow(Kernel.Instance.JobProfiles.JbhInfoDates(), Kernel.Instance.JobProfiles.JbhBusinessDates())
            {
                Owner = this
            };
            w.ShowDialog();
        }
        
        private void FamilyMatButton_Click(object sender, RoutedEventArgs e)
        {
            using (FamilyPublishWindow w = new FamilyPublishWindow(false) { Owner = this })
            {
                w.ShowDialog();
            }
        }

        private void FamilyPatButton_Click(object sender, RoutedEventArgs e)
        {
            using (FamilyPublishWindow w = new FamilyPublishWindow(true) { Owner = this })
            {
                w.ShowDialog();
            }
        }

        private void ListInfoButton_Click(object sender, RoutedEventArgs e)
        {
            lmode = ListMode.Info;
            SelectionTextBlock.Text = "Jbh.Info backup jobs";
            RefreshLists();
        }
        private void ListBusinesButton_Click(object sender, RoutedEventArgs e)
        {
            lmode = ListMode.Business;
            SelectionTextBlock.Text = "Jbh.Business backup jobs";
            RefreshLists();
        }
        private void ListOtherButton_Click(object sender, RoutedEventArgs e)
        {
            lmode = ListMode.Other;
            SelectionTextBlock.Text = "Miscellaneous jobs";
            RefreshLists();
        }
    }
}