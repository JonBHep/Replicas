﻿using System;
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

internal partial class FamilyPublishWindow
    {
        private readonly bool _paternity;
        private string? _sourceFolder;
        private string? _targetFolder;
        private long _expectedCount;
        UpdaterResults? _results;
        private string? _sourceFolderMat;
        private string? _targetFolderMat;
        private long _expectedCountMat;
        private string? _sourceFolderPat;
        private string? _targetFolderPat;
        private long _expectedCountPat;
        private Progress<ProgressInfo>? _progressChaser;
        private CancellationTokenSource? _cts;
        private ReplicaJobTasks? _tasks;
        private int _lastUpdateProgressReport;
        private bool _runOn; // whether to run straight on from analysis to perform the update

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

        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayMessage("Ready to analyse", waitingInput: true);
            TextBlockProgressSource.Text = string.Empty;
            ButtonUpdate.Visibility = Visibility.Collapsed;
            ButtonDetail.Visibility = Visibility.Collapsed;

            ButtonClose.Visibility = Visibility.Visible;
            ButtonClose.Content = "Cancel";
            ButtonCancelUpdate.Visibility = Visibility.Collapsed;

            SwitchImplementationDisplay(false);
            SwitchImplementationErrorsDisplay(false);
            _progressChaser = new Progress<ProgressInfo>(info => { DisplayProgress(info); });

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonUpdate.IsVisible)
            {
                if (MessageBox.Show("You have not performed the update\n\nClose anyway?", "Replicate", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) { Close(); }
            }
            else
            { Close(); }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ButtonClose.Visibility != Visibility.Visible)
            {
                MessageBox.Show("JBH: I am not allowing window closure because the Close button is not visible", "Replicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }
            SaveSettings();
        }

        private void ImplementUpdate(string SourceRootDir, string DestinationRootDir, ReplicaJobTasks jobs, IProgress<ProgressInfo> prog, CancellationToken cancellationToken)
        {
            _results = new UpdaterResults();
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
                    FileInfo dFile = new FileInfo(act.DestinationPath);
                    FileAttributes fileAttribs = dFile.Attributes;
                    
                    if ((fileAttribs != 0) && (fileAttribs != FileAttributes.Normal))
                    {
                        string erreur = Kernel.AttemptToSetFileAttributes(act.DestinationPath, FileAttributes.Normal);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText = "Failed to clear file read-only attribute: " + erreur; }
                    }

                    string faute = Kernel.AttemptToDeleteFile(act.DestinationPath);
                    if (string.IsNullOrEmpty(faute))
                    {
                        _results.FileDelSuccess++;
                    }
                    else
                    {
                        act.ErrorText = faute;
                        _results.FileDelFailure++;
                    }

                    SendUpdateProgressReport(progressCounter, progressTarget, _results, prog);
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
                        _results.DirDelSuccess++;
                    }
                    else
                    {
                        act.ErrorText = faute;
                        _results.DirDelFailure++;
                    }
                    SendUpdateProgressReport(progressCounter, progressTarget, _results, prog);
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
                        _results.DirAddSuccess++;
                    }
                    else
                    {
                        act.ErrorText =faute;
                        _results.DirAddFailure++;
                    }
                    SendUpdateProgressReport(progressCounter, progressTarget, _results, prog);
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
                    FileInfo dFile = new FileInfo(act.DestinationPath);
                    FileAttributes fileAttribs = dFile.Attributes;
                    if ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        fileAttribs &= ~FileAttributes.ReadOnly; // remove ReadOnly attribute
                        string erreur = Kernel.AttemptToSetFileAttributes(act.DestinationPath, fileAttribs);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText= "Failed to clear file read-only attribute: " + erreur; }
                    }

                    // Remove any Archive attribute from source file
                    FileInfo sFile = new FileInfo(act.SourcePath(destinationRoot, sourceRoot));
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
                        _results.FileUpdSuccess++;
                    }
                    else
                    {
                        act.ErrorText = "Failed to update file: " + faute;
                        _results.FileUpdFailure++;
                    }
                    SendUpdateProgressReport(progressCounter, progressTarget, _results, prog);
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
                    FileInfo sFile = new FileInfo(act.SourcePath(destinationRoot, sourceRoot));
                    FileAttributes fileAttribs = sFile.Attributes;
                    if ((fileAttribs & FileAttributes.Archive) == FileAttributes.Archive)
                    {
                        fileAttribs &= ~FileAttributes.Archive; // remove Archive attribute
                        string erreur = Kernel.AttemptToSetFileAttributes(act.SourcePath(destinationRoot, sourceRoot), fileAttribs);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText = "Failed to clear file archive attribute: " + erreur; }
                    }

                    try
                    { //3//
                        File.Copy(act.SourcePath(destinationRoot, sourceRoot), act.DestinationPath, overwrite: true);
                        _results.FileAddSuccess++;
                    } //3//
                    catch (UnauthorizedAccessException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        _results.FileAddFailure++;
                    }
                    catch (ArgumentException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        _results.FileAddFailure++;
                    }
                    catch (PathTooLongException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        _results.FileAddFailure++;
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        _results.FileAddFailure++;
                    }
                    catch (FileNotFoundException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        _results.FileAddFailure++;
                    }
                    catch (IOException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        _results.FileAddFailure++;
                    }
                    catch (NotSupportedException ex)
                    {
                        act.ErrorText = "Failed to add file: " + ex.Message;
                        _results.FileAddFailure++;
                    }
                    SendUpdateProgressReport(progressCounter, progressTarget, _results, prog);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
            }
        finishing:
            // send a final progress report to ensure that the final display is up-to-date
            SendUpdateProgressReport(progressCounter, progressTarget, _results, prog);
            if (cancellationToken.IsCancellationRequested) { _results.WasCancelled = true; }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchUpdate();
        }

        private async void LaunchUpdate()
        {
            ButtonUpdate.Visibility = Visibility.Collapsed;
            ButtonDetail.Visibility = Visibility.Collapsed;
            ButtonCancelUpdate.Visibility = Visibility.Visible;
            DisplayMessage("Updating", waitingInput: false);
            ButtonClose.Visibility = Visibility.Collapsed;
            Cursor = Cursors.Wait;

            // NB This implementation polls CancellationToken.IsCancellationRequested (bool) rather than throwing an error
            // Compare the example in FamilyTree - detectIntramarriages which uses the error method (which Cleary favours)
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            await Task.Run(() => ImplementUpdate(_sourceFolder, _targetFolder, _tasks, _progressChaser, token)).ConfigureAwait(true);
            Kernel.SoundSignal(600, 500);
            PostUpdateDisplay(_results);

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
            ButtonAnalyse.Visibility = Visibility.Collapsed;
            ButtonAnalysePlus.Visibility = Visibility.Collapsed;
            if (source == ButtonAnalyse) { _runOn = false; } else { _runOn = true; }

            Cursor = Cursors.Wait;
            ButtonClose.Visibility = Visibility.Collapsed;
            DisplayMessage("Analysing", waitingInput: false);

            FamilyAnalysis _analysis = new FamilyAnalysis(sourceRootDir: _sourceFolder, destinationRootDir: _targetFolder, expectedItemCount: _expectedCount, _progressChaser);

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
            if (waitingInput) { TextBlockMessage.Foreground = Brushes.DarkRed; } else { TextBlockMessage.Foreground = Brushes.ForestGreen; }
            TextBlockMessage.Text = phase;
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

            ProgressbarSource.Value = 0;
            TextBlockProgressSource.Text = string.Empty;

            ProgressbarDestination.Value = 0;
            TextBlockProgressDestination.Text = string.Empty;

            ProgressbarUpdate.Value = 0;
            TextBlockProgressUpdate.Text = string.Empty;

           
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

            Cursor = Cursors.Arrow;

            string f = "Some source files are older than the corresponding destination files.\nThis may mean that the destination directory has been updated more recently than the source directory.\nIt could also result from files being renamed.\nAll files of different date or size will be overwritten by source files.\nDo not update unless confident that you want to replace newer files with older.";
            if (_anal.OlderSourceWarnings.Count > 0)
            {
                WarningsWindow ww = new WarningsWindow();
                ww.SetCaption("Source older than destination");
                ww.SetComment(f);
                ww.LstExamples.Items.Clear();
                foreach (string g in _anal.OlderSourceWarnings) { ww.LstExamples.Items.Add(g); }
                ww.ShowDialog();
                _runOn = false; // don't automatically run the update if queries have been raised
            }

            if (_anal.JobTaskBundle.GrandTotal > 0)
            {
                if (_anal.JobTaskBundle.PathAndFilenameLengthsOk())
                {
                    DisplayMessage("Ready to update", waitingInput: true);
                    ButtonUpdate.IsEnabled = true;
                    ButtonUpdate.Visibility = Visibility.Visible;
                    ButtonDetail.Tag = _anal.JobTaskBundle;
                    ButtonDetail.Visibility = Visibility.Visible;
                    if (_runOn)
                    {
                        LaunchUpdate();
                    }
                    else
                    {
                        ButtonClose.Content = "Cancel";
                        ButtonClose.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    DisplayMessage("Filename or path lengths exceeded", waitingInput: true);
                    ButtonDetail.Visibility = Visibility.Visible;
                    ButtonClose.Content = "Close";
                    ButtonClose.Visibility = Visibility.Visible;
                }
            }
            else
            {
                DisplayMessage("Nothing to do!", waitingInput: true);
                ButtonClose.Content = "Close";
                ButtonClose.Visibility = Visibility.Visible;
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
            ButtonCancelUpdate.Visibility = Visibility.Collapsed;

            DisplayStatistics(results);

            if (results.WasCancelled)
            {
                DisplayMessage("Update cancelled", waitingInput: true);
            }
            else
            {
                DisplayMessage("Finished", waitingInput: true);
            }

            ButtonDetail.Visibility = Visibility.Visible;
            ButtonClose.Content = "Finish";
            ButtonClose.Visibility = Visibility.Visible;
            ButtonClose.Focus();
            Cursor = Cursors.Arrow;

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
            ButtonCancelUpdate.IsEnabled = false;
            DisplayMessage("Cancel requested...", waitingInput: false);
        }

        private void SwitchImplementationDisplay(bool flag)
        {
            Visibility vis = Visibility.Hidden;
            if (flag) { vis = Visibility.Visible; }

            ActionsToDoTBk.Visibility = vis;
            ActionsDoneTBk.Visibility = vis;

            LblActionFd.Visibility = vis;
            LblActionDd.Visibility = vis;
            LblActionDa.Visibility = vis;
            LblActionFa.Visibility = vis;
            LblActionFu.Visibility = vis;

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
                        ProgressbarSource.Value = i.PercentNumber;
                        TextBlockProgressSource.Text = $"{i.PercentNumber}%";
                        break;
                    }
                case 'D':
                    {
                        ProgressbarDestination.Value = i.PercentNumber;
                        TextBlockProgressDestination.Text = $"{i.PercentNumber}%";
                        break;
                    }
                case 'U':
                    {
                        ProgressbarUpdate.Value = i.PercentNumber;
                        TextBlockProgressUpdate.Text = $"{i.PercentNumber}%";
                        DisplayStatistics(i.Results);
                        break;
                    }
            }
        }
    
}