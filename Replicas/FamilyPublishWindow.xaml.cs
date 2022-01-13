using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Replicas;

internal partial class FamilyPublishWindow : IDisposable
    {
        private bool disposed;
        private readonly bool _paternity;
        private string _sourceFolder;
        private string _targetFolder;
        private long _expectedCount;
        UpdaterResults results;
        private string _sourceFolderMat;
        private string _targetFolderMat;
        private long _expectedCountMat;
        private string _sourceFolderPat;
        private string _targetFolderPat;
        private long _expectedCountPat;
        private Progress<ProgressInfo> _progressChaser;
        private CancellationTokenSource _cts;

        public FamilyPublishWindow(bool pat)
        {
            InitializeComponent();
            _paternity = pat;
        }

        private static string SettingsFile
        {
            get
            {
                string p = Jbh.AppManager.DataPath;
                p = Path.Combine(p, "FamilySettings.jbh");
                return p;
            }
        }
        private void LoadSettings()
        {
            string path = SettingsFile;
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    _sourceFolderMat = sr.ReadLine();
                    _targetFolderMat = sr.ReadLine();
                    string l = sr.ReadLine();
                    _expectedCountMat = long.Parse(l, CultureInfo.InvariantCulture);
                    _sourceFolderPat = sr.ReadLine();
                    _targetFolderPat = sr.ReadLine();
                    l = sr.ReadLine();
                    _expectedCountPat = long.Parse(l, CultureInfo.InvariantCulture);
                }
                if (_paternity)
                {
                    _sourceFolder = _sourceFolderPat;
                    _targetFolder = _targetFolderPat;
                    _expectedCount = _expectedCountPat;
                }
                else
                {
                    _sourceFolder = _sourceFolderMat;
                    _targetFolder = _targetFolderMat;
                    _expectedCount = _expectedCountMat;
                }
            }
        }

        private void SaveSettings()
        {
            // in case changed
            if (_paternity)
            {
                _sourceFolderPat = _sourceFolder;
                _targetFolderPat = _targetFolder;
                _expectedCountPat = _expectedCount;
            }
            else
            {
                _sourceFolderMat = _sourceFolder;
                _targetFolderMat = _targetFolder;
                _expectedCountMat = _expectedCount;
            }

            string path = SettingsFile;
            Jbh.AppManager.CreateBackupDataFile(path);
            Jbh.AppManager.PurgeOldBackups("jbh", 5, 5);
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine(_sourceFolderMat);
                sw.WriteLine(_targetFolderMat);
                sw.WriteLine(_expectedCountMat.ToString(CultureInfo.InvariantCulture));
                sw.WriteLine(_sourceFolderPat);
                sw.WriteLine(_targetFolderPat);
                sw.WriteLine(_expectedCountPat.ToString( CultureInfo.InvariantCulture));
            }
        }

        private void SelectSourceButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowser browser = new FolderBrowser() { Owner = this };
            bool? Q = browser.ShowDialog();
            if (Q.HasValue && Q.Value)
            {
                _sourceFolder = browser.SelectedDirectory;
                SourceTextBlock.Text = _sourceFolder;
            }
        }

        private void SelectTargetButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowser browser = new FolderBrowser() { Owner = this };
            bool? Q = browser.ShowDialog();
            if (Q.HasValue && Q.Value)
            {
                _targetFolder = browser.SelectedDirectory;
                TargetTextBlock.Text = _targetFolder;
            }
        }

        private ReplicaJobTasks _tasks;
        private int _lastUpdateProgressReport;
        private bool _runOn; // whether to run straight on from analysis to perform the update
        internal bool Fulfilled;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayMessage("Ready to analyse", waitingInput: true);
            textblockProgressSource.Text = string.Empty;
            buttonUpdate.Visibility = Visibility.Collapsed;
            buttonDetail.Visibility = Visibility.Collapsed;

            buttonClose.Visibility = Visibility.Visible;
            buttonClose.Content = "Cancel";
            buttonCancelUpdate.Visibility = Visibility.Collapsed;

            SwitchImplementationDisplay(false);
            SwitchImplementationErrorsDisplay(false);
            _progressChaser = new Progress<ProgressInfo>(info => { DisplayProgress(info); });

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (buttonUpdate.IsVisible)
            {
                if (MessageBox.Show("You have not performed the update\n\nClose anyway?", "Replicate", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) { Close(); }
            }
            else
            { Close(); }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (buttonClose.Visibility != Visibility.Visible)
            {
                MessageBox.Show("JBH: I am not allowing window closure because the Close button is not visible", "Replicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }
            SaveSettings();
        }

        private void ImplementUpdate(string SourceRootDir, string DestinationRootDir, ReplicaJobTasks jobs, IProgress<ProgressInfo> prog, CancellationToken cancellationToken)
        {
            results = new UpdaterResults();
            string destinationRoot = DestinationRootDir;
            string sourceRoot = SourceRootDir;
            long progressTarget = jobs.TotalBulk();
            long progressCounter = 0;

            List<ReplicaAction> actionlistFD = jobs.TaskList("FD");
            List<ReplicaAction> actionlistDD = jobs.TaskList("DD");
            List<ReplicaAction> actionlistDA = jobs.TaskList("DA");
            List<ReplicaAction> actionlistFU = jobs.TaskList("FU");
            List<ReplicaAction> actionlistFA = jobs.TaskList("FA");

            if (actionlistFD.Count > 0)
            {
                foreach (ReplicaAction act in actionlistFD)
                {
                    progressCounter += act.Bulk;
                    // Remove any attributes from the destination file
                    System.IO.FileInfo dFile = new System.IO.FileInfo(act.DestinationPath);
                    System.IO.FileAttributes fileAttribs = dFile.Attributes;
                    
                    if ((fileAttribs != 0) && (fileAttribs != FileAttributes.Normal))
                    {
                        string erreur = Kernel.AttemptToSetFileAttributes(act.DestinationPath, FileAttributes.Normal);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText = "Failed to clear file read-only attribute: " + erreur; }
                    }

                    string faute = Kernel.AttemptToDeleteFile(act.DestinationPath);
                    if (string.IsNullOrEmpty(faute))
                    {
                        results.FileDelSuccess++;
                    }
                    else
                    {
                        act.ErrorText = faute;
                        results.FileDelFailure++;
                    }

                    SendUpdateProgressReport(progressCounter, progressTarget, results, prog);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionlistDD.Count > 0)
            {
                foreach (ReplicaAction act in actionlistDD)
                {
                    progressCounter += act.Bulk;
                    string faute = Kernel.AttemptToDeleteDirectory(act.DestinationPath);
                    if (string.IsNullOrEmpty(faute))
                    {
                        results.DirDelSuccess++;
                    }
                    else
                    {
                        act.ErrorText = faute;
                        results.DirDelFailure++;
                    }
                    SendUpdateProgressReport(progressCounter, progressTarget, results, prog);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionlistDA.Count > 0)
            {
                foreach (ReplicaAction act in actionlistDA)
                {
                    progressCounter += act.Bulk;
                    string faute = Kernel.AttemptToAddDirectory(act.DestinationPath);
                    if (string.IsNullOrEmpty(faute))
                    {
                        results.DirAddSuccess++;
                    }
                    else
                    {
                        act.ErrorText =faute;
                        results.DirAddFailure++;
                    }
                    SendUpdateProgressReport(progressCounter, progressTarget, results, prog);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionlistFU.Count > 0)
            {
                foreach (ReplicaAction act in actionlistFU)
                {
                    progressCounter += act.Bulk;
                    
                    // Remove any ReadOnly attribute from destination file
                    System.IO.FileInfo dFile = new System.IO.FileInfo(act.DestinationPath);
                    System.IO.FileAttributes fileAttribs = dFile.Attributes;
                    if ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        fileAttribs &= ~FileAttributes.ReadOnly; // remove ReadOnly attribute
                        string erreur = Kernel.AttemptToSetFileAttributes(act.DestinationPath, fileAttribs);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText= "Failed to clear file read-only attribute: " + erreur; }
                    }

                    // Remove any Archive attribute from source file
                    System.IO.FileInfo sFile = new System.IO.FileInfo(act.SourcePath(destinationRoot, sourceRoot));
                    fileAttribs = sFile.Attributes;
                    if ((fileAttribs & FileAttributes.Archive) == FileAttributes.Archive)
                    {
                        fileAttribs &= ~FileAttributes.Archive; // remove Archive attribute
                        string erreur = Kernel.AttemptToSetFileAttributes(act.SourcePath(destinationRoot, sourceRoot), fileAttribs);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText = "Failed to clear file archive attribute: " + erreur; }
                    }

                    string faute = Kernel.AttemptToCopyFile(act.SourcePath(destinationRoot, sourceRoot), act.DestinationPath, overwrite: true);
                    if (string.IsNullOrEmpty(faute))
                    {
                        results.FileUpdSuccess++;
                    }
                    else
                    {
                        act.ErrorText = "Failed to update file: " + faute;
                        results.FileUpdFailure++;
                    }
                    SendUpdateProgressReport(progressCounter, progressTarget, results, prog);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionlistFA.Count > 0)
            {
                foreach (ReplicaAction act in actionlistFA)
                {
                    progressCounter += act.Bulk;

                    // Remove any Archive attribute from source file
                    System.IO.FileInfo sFile = new System.IO.FileInfo(act.SourcePath(destinationRoot, sourceRoot));
                    System.IO.FileAttributes fileAttribs = sFile.Attributes;
                    if ((fileAttribs & FileAttributes.Archive) == FileAttributes.Archive)
                    {
                        fileAttribs &= ~FileAttributes.Archive; // remove Archive attribute
                        string erreur = Kernel.AttemptToSetFileAttributes(act.SourcePath(destinationRoot, sourceRoot), fileAttribs);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText = "Failed to clear file archive attribute: " + erreur; }
                    }

                    try
                    { //3//
                        System.IO.File.Copy(act.SourcePath(destinationRoot, sourceRoot), act.DestinationPath, overwrite: true);
                        results.FileAddSuccess++;
                    } //3//
                    catch (UnauthorizedAccessException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        results.FileAddFailure++;
                    }
                    catch (ArgumentException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        results.FileAddFailure++;
                    }
                    catch (PathTooLongException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        results.FileAddFailure++;
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        results.FileAddFailure++;
                    }
                    catch (FileNotFoundException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        results.FileAddFailure++;
                    }
                    catch (IOException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        results.FileAddFailure++;
                    }
                    catch (NotSupportedException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        results.FileAddFailure++;
                    }
                    SendUpdateProgressReport(progressCounter, progressTarget, results, prog);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
            }
        finishing:
            // send a final progress report to ensure that the final display is up-to-date
            SendUpdateProgressReport(progressCounter, progressTarget, results, prog);
            if (cancellationToken.IsCancellationRequested) { results.WasCancelled = true; }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchUpdate();
        }

        private async void LaunchUpdate()
        {
            buttonUpdate.Visibility = Visibility.Collapsed;
            buttonDetail.Visibility = Visibility.Collapsed;
            buttonCancelUpdate.Visibility = Visibility.Visible;
            DisplayMessage("Updating", waitingInput: false);
            buttonClose.Visibility = Visibility.Collapsed;
            this.Cursor = Cursors.Wait;

            // NB This implementation polls CancellationToken.IsCancellationRequested (bool) rather than throwing an error
            // Compare the example in FamilyTree - detectIntramarriages which uses the error method (which Cleary favours)
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            await Task.Run(() => ImplementUpdate(_sourceFolder, _targetFolder, _tasks, _progressChaser, token)).ConfigureAwait(true);
            Kernel.SoundSignal(600, 500);
            PostUpdateDisplay(results);

        }

        private async void Analyse_Click(object sender, RoutedEventArgs e)
        {
            // can be triggered by 'Analyse' button or 'Analyse and Update' button 

            if ((!Directory.Exists(_sourceFolder)) || (!Directory.Exists(_targetFolder)))
            {
                MessageBox.Show("Either the source or target directory does not exist", "Reselect paths", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Button source = sender as Button;
            buttonAnalyse.Visibility = Visibility.Collapsed;
            buttonAnalysePlus.Visibility = Visibility.Collapsed;
            if (source == buttonAnalyse) { _runOn = false; } else { _runOn = true; }

            this.Cursor = Cursors.Wait;
            buttonClose.Visibility = Visibility.Collapsed;
            DisplayMessage("Analysing", waitingInput: false);

            FamilyAnalysis _analysis = new FamilyAnalysis(SourceRootDir: _sourceFolder, DestinationRootDir: _targetFolder, ExpectedItemCount: _expectedCount, _progressChaser);

            await Task.Run(() => _analysis.PerformAnalysis()).ConfigureAwait(true);

            if (_paternity) { _expectedCountPat = _analysis.ExpectedItemCount; } else { _expectedCountMat = _analysis.ExpectedItemCount; }

            PostAnalysisDisplay(_analysis);
            Kernel.SoundSignal(300, 500);
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            string root = _targetFolder;
            ActionDetailsWindow w = new ActionDetailsWindow(_tasks, root.Length)
            {
                Owner = this
            };
            w.ShowDialog();
        }

        private void DisplayMessage(string phase, bool waitingInput)
        {
            if (waitingInput) { textblockMessage.Foreground = Brushes.DarkRed; } else { textblockMessage.Foreground = Brushes.ForestGreen; }
            textblockMessage.Text = phase;
        }

        private void SetFonts()
        {
            // sets font for numeric displays to one in which digits are fixed width not proportional
            FontFamily fam = new FontFamily("Microsoft Sans Serif");
            double siz = 12;

            FilesToDeleteTBk.FontFamily = fam;
            FilesToDeleteTBk.FontSize = siz;
            FilesDeletedTBk.FontFamily = fam;
            FilesDeletedTBk.FontSize = siz;
            FileDeleteErrorsTBk.FontFamily = fam;
            FileDeleteErrorsTBk.FontSize = siz;

            DirectoriesToDeleteTBk.FontFamily = fam;
            DirectoriesToDeleteTBk.FontSize = siz;
            DirectoriesDeletedTBk.FontFamily = fam;
            DirectoriesDeletedTBk.FontSize = siz;
            DirectoryDeleteErrorsTBk.FontFamily = fam;
            DirectoryDeleteErrorsTBk.FontSize = siz;

            DirectoriesToAddTBk.FontFamily = fam;
            DirectoriesToAddTBk.FontSize = siz;
            DirectoriesAddedTBk.FontFamily = fam;
            DirectoriesAddedTBk.FontSize = siz;
            DirectoryAddErrorsTBk.FontFamily = fam;
            DirectoryAddErrorsTBk.FontSize = siz;

            FilesToUpdateTBk.FontFamily = fam;
            FilesToUpdateTBk.FontSize = siz;
            FilesUpdatedTBk.FontFamily = fam;
            FilesUpdatedTBk.FontSize = siz;
            FileUpdateErrorsTBk.FontFamily = fam;
            FileUpdateErrorsTBk.FontSize = siz;

            FilesToAddTBk.FontFamily = fam;
            FilesToAddTBk.FontSize = siz;
            FilesAddedTBk.FontFamily = fam;
            FilesAddedTBk.FontSize = siz;
            FileAddErrorsTBk.FontFamily = fam;
            FileAddErrorsTBk.FontSize = siz;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LoadSettings();
            SourceTextBlock.Text = _sourceFolder;
            TargetTextBlock.Text = _targetFolder;

            SetFonts();

            progressbarSource.Value = 0;
            textblockProgressSource.Text = string.Empty;

            progressbarDestination.Value = 0;
            textblockProgressDestination.Text = string.Empty;

            progressbarUpdate.Value = 0;
            textblockProgressUpdate.Text = string.Empty;

           
        }

        private void PostAnalysisDisplay(FamilyAnalysis _anal)
        {
            _tasks = _anal.JobTaskBundle;

            FilesToAddTBk.Text = _anal.JobTaskBundle.TaskCount("FA").ToString(CultureInfo.InvariantCulture);
            DirectoriesToAddTBk.Text = _anal.JobTaskBundle.TaskCount("DA").ToString(CultureInfo.InvariantCulture);
            FilesToDeleteTBk.Text = _anal.JobTaskBundle.TaskCount("FD").ToString(CultureInfo.InvariantCulture);
            DirectoriesToDeleteTBk.Text = _anal.JobTaskBundle.TaskCount("DD").ToString(CultureInfo.InvariantCulture);
            FilesToUpdateTBk.Text = _anal.JobTaskBundle.TaskCount("FU").ToString(CultureInfo.InvariantCulture);

            SwitchImplementationDisplay(true);

            this.Cursor = Cursors.Arrow;

            string f = "Some source files are older than the corresponding destination files.\nThis may mean that the destination directory has been updated more recently than the source directory.\nIt could also result from files being renamed.\nAll files of different date or size will be overwritten by source files.\nDo not update unless confident that you want to replace newer files with older.";
            if (_anal.OlderSourceWarnings.Count > 0)
            {
                WarningsWindow ww = new WarningsWindow();
                ww.SetCaption("Source older than destination");
                ww.SetComment(f);
                ww.lstExamples.Items.Clear();
                foreach (string g in _anal.OlderSourceWarnings) { ww.lstExamples.Items.Add(g); }
                ww.ShowDialog();
                _runOn = false; // don't automatically run the update if queries have been raised
            }

            if (_anal.JobTaskBundle.GrandTotal > 0)
            {
                if (_anal.JobTaskBundle.PathAndFilenameLengthsOK())
                {
                    DisplayMessage("Ready to update", waitingInput: true);
                    buttonUpdate.IsEnabled = true;
                    buttonUpdate.Visibility = Visibility.Visible;
                    buttonDetail.Tag = _anal.JobTaskBundle;
                    buttonDetail.Visibility = Visibility.Visible;
                    if (_runOn)
                    {
                        LaunchUpdate();
                    }
                    else
                    {
                        buttonClose.Content = "Cancel";
                        buttonClose.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    DisplayMessage("Filename or path lengths exceeded", waitingInput: true);
                    Fulfilled = false;
                    buttonDetail.Visibility = Visibility.Visible;
                    buttonClose.Content = "Close";
                    buttonClose.Visibility = Visibility.Visible;
                }
            }
            else
            {
                DisplayMessage("Nothing to do!", waitingInput: true);
                Fulfilled = true;
                buttonClose.Content = "Close";
                buttonClose.Visibility = Visibility.Visible;
            }
        }

        private void DisplayStatistics(UpdaterResults results)
        {
            FilesDeletedTBk.Text = results.FileDelSuccess.ToString(CultureInfo.InvariantCulture);
            FileDeleteErrorsTBk.Text = results.FileDelFailure.ToString(CultureInfo.InvariantCulture);

            DirectoriesDeletedTBk.Text = results.DirDelSuccess.ToString(CultureInfo.InvariantCulture);
            DirectoryDeleteErrorsTBk.Text = results.DirDelFailure.ToString(CultureInfo.InvariantCulture);

            DirectoriesAddedTBk.Text = results.DirAddSuccess.ToString(CultureInfo.InvariantCulture);
            DirectoryAddErrorsTBk.Text = results.DirAddFailure.ToString(CultureInfo.InvariantCulture);

            FilesUpdatedTBk.Text = results.FileUpdSuccess.ToString(CultureInfo.InvariantCulture);
            FileUpdateErrorsTBk.Text = results.FileUpdFailure.ToString(CultureInfo.InvariantCulture);

            FilesAddedTBk.Text = results.FileAddSuccess.ToString(CultureInfo.InvariantCulture);
            FileAddErrorsTBk.Text = results.FileAddFailure.ToString(CultureInfo.InvariantCulture);
        }

        private void PostUpdateDisplay(UpdaterResults results)
        {
            buttonCancelUpdate.Visibility = Visibility.Collapsed;

            DisplayStatistics(results);

            if (results.WasCancelled)
            {
                DisplayMessage("Update cancelled", waitingInput: true);
            }
            else
            {
                Fulfilled = true;
                DisplayMessage("Finished", waitingInput: true);
            }

            buttonDetail.Visibility = Visibility.Visible;
            buttonClose.Content = "Finish";
            buttonClose.Visibility = Visibility.Visible;
            buttonClose.Focus();
            this.Cursor = Cursors.Arrow;

            if (results.AnyFailures)
            {
                SwitchImplementationErrorsDisplay(true);
                if (MessageBox.Show("There were errors - do you want to view the action details?", "Replicate", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    string root = _targetFolder;
                    ActionDetailsWindow w = new ActionDetailsWindow(_tasks, root.Length)
                    {
                        Owner = this
                    };
                    w.ShowDialog();
                }
            }
        }

        private void CancelUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            buttonCancelUpdate.IsEnabled = false;
            DisplayMessage("Cancel requested...", waitingInput: false);
        }

        private void SwitchImplementationDisplay(bool flag)
        {
            Visibility vis = Visibility.Hidden;
            if (flag) { vis = Visibility.Visible; }

            ActionsToDoTBk.Visibility = vis;
            ActionsDoneTBk.Visibility = vis;

            lblActionFD.Visibility = vis;
            lblActionDD.Visibility = vis;
            lblActionDA.Visibility = vis;
            lblActionFA.Visibility = vis;
            lblActionFU.Visibility = vis;

            FilesToDeleteTBk.Visibility = vis;
            FilesDeletedTBk.Visibility = vis;
            DirectoriesToDeleteTBk.Visibility = vis;
            DirectoriesDeletedTBk.Visibility = vis;
            DirectoriesToAddTBk.Visibility = vis;
            DirectoriesAddedTBk.Visibility = vis;
            FilesToAddTBk.Visibility = vis;
            FilesAddedTBk.Visibility = vis;
            FilesToUpdateTBk.Visibility = vis;
            FilesUpdatedTBk.Visibility = vis;
        }

        private void SwitchImplementationErrorsDisplay(bool flag)
        {
            Visibility vis = Visibility.Hidden;
            if (flag) { vis = Visibility.Visible; }

            ActionsFailedTBk.Visibility = vis;

            FileDeleteErrorsTBk.Visibility = vis;
            DirectoryDeleteErrorsTBk.Visibility = vis;
            DirectoryAddErrorsTBk.Visibility = vis;
            FileAddErrorsTBk.Visibility = vis;
            FileUpdateErrorsTBk.Visibility = vis;
        }

        private void SendUpdateProgressReport(long progressValue, long progressTarget, UpdaterResults report, IProgress<ProgressInfo> prog)
        {
            int ProgressReport = Convert.ToInt32(((double)progressValue / progressTarget) * 100);
            if (ProgressReport > 100) { ProgressReport = 100; } // don't exceed 100%
            if (ProgressReport != _lastUpdateProgressReport)
            {
                ProgressInfo info = new ProgressInfo(0, ProgressReport, 'U', report);
                prog.Report(info);
                _lastUpdateProgressReport = ProgressReport;
            }
        }
        
        void DisplayProgress(ProgressInfo i)
        {
            char Q = i.Phase;
            switch (Q)
            {
                case 'S':
                    {
                        progressbarSource.Value = i.PercentNumber;
                        textblockProgressSource.Text = $"{i.PercentNumber}%";
                        break;
                    }
                case 'D':
                    {
                        progressbarDestination.Value = i.PercentNumber;
                        textblockProgressDestination.Text = $"{i.PercentNumber}%";
                        break;
                    }
                case 'U':
                    {
                        progressbarUpdate.Value = i.PercentNumber;
                        textblockProgressUpdate.Text = $"{i.PercentNumber}%";
                        DisplayStatistics(i.Results);
                        break;
                    }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (!(_cts is null))
                    {
                        _cts.Dispose(); // (managed objects)
                    }
                }
                disposed = true;
            }
        }

        
        ~FamilyPublishWindow()
        {
             // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
             Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    
}