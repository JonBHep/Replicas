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

internal partial class RunWindow : Window
{
    
    private Tache boulot;
        private bool disposed;
        private ReplicaJobTasks tasks;
        private UpdaterResults results;
        private bool includeHidden;
        private bool runOn; // whether to run straight on from analysis to perform the update
        private long freespacebefore;
        internal bool Fulfilled;
        private Progress<ProgressInfo> progressChaser;
        private CancellationTokenSource cts;
        internal RunWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // hide action count labels

            stackpanelafter.Visibility = Visibility.Hidden;

            DisplayMessage("Ready to analyse", waitingInput: true);
            textblockProgressSource.Text = string.Empty;
            lblFileBalance.Text = "";
            lblFolderBalance.Text = "";
            buttonUpdate.Visibility = Visibility.Collapsed;
            buttonDetail.Visibility = Visibility.Collapsed;
            boulot = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask];
            txtTitle.Text = "Replicate - " +boulot.JobTitle;

            includeHidden =boulot.IncludeHidden;
            lblPathS.Text =boulot.SourceFoundDrivePath();
            lblPathD.Text =boulot.DestinationFoundDrivePath();
            string v =boulot.SourceVolume;
            lblDriveS.Text = Kernel.Instance.KnownDrives.VolumeDescription(v) + " (" + v + ")";
            v =boulot.DestinationVolume;
            lblDriveD.Text = Kernel.Instance.KnownDrives.VolumeDescription(v) + " (" + v + ")";

            lblSourceFilesL.Visibility = Visibility.Hidden;
            lblSourceFiles.Visibility = Visibility.Hidden;
            lblDestinFilesL.Visibility = Visibility.Hidden;
            lblDestinFiles.Visibility = Visibility.Hidden;
            lblSourceFoldersL.Visibility = Visibility.Hidden;
            lblSourceFolders.Visibility = Visibility.Hidden;
            lblDestinFoldersL.Visibility = Visibility.Hidden;
            lblDestinFolders.Visibility = Visibility.Hidden;
            lblFileBalance.Visibility = Visibility.Hidden;
            lblFolderBalance.Visibility = Visibility.Hidden;

            lblLastPerformed.Text =boulot.PeriodSinceLastRun();
            if (includeHidden) { lblHidden.Text = "Included"; } else { lblHidden.Text = "Not included"; }

            // report source and destination usage before backup
            long tot = boulot.SourceSize();
            long fre =boulot.SourceFree();
            int percentfree = Convert.ToInt32(100 * ((double)fre / tot));
            lblSourceScope.Text = $"{Kernel.SizeReport(tot)} total {Kernel.SizeReport(fre)} free ({percentfree}% free)";
            prgSource.Value = 100 - percentfree;

            tot =boulot.DestinationSize();
            fre =boulot.DestinationFree();
            freespacebefore = fre;
            percentfree = Convert.ToInt32(100 * ((double)fre / tot));
            lblDestinationAfterScope.Text = lblDestinationBeforeScope.Text = $"{Kernel.SizeReport(tot)} total {Kernel.SizeReport(fre)} free ({percentfree}% free)";
            prgDestinationAfter.Value = prgDestinationBefore.Value = 100 - percentfree;

            buttonClose.Visibility = Visibility.Visible;
            buttonClose.Content = "Cancel";
            buttonCancelUpdate.Visibility = Visibility.Collapsed;

            SwitchImplementationDisplay(false);
            SwitchImplementationErrorsDisplay(false); 

            progressChaser = new Progress<ProgressInfo>(info => { DisplayAnalysisProgress(info); });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (buttonUpdate.IsVisible)
            {
                if (MessageBox.Show("You have not performed the update\n\nClose anyway?", "Replicas", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) { Close(); }
            }
            else
            { Close(); }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (buttonClose.Visibility != Visibility.Visible)
            {
                MessageBox.Show("JBH: I am not allowing window closure because the Close button is not visible", "Replicas", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }
        }

        private void ImplementUpdate(string SourceRootDir, string DestinationRootDir, ReplicaJobTasks jobs, IProgress<ProgressInfo> prog, CancellationToken cancellationToken)
        {
            results = new UpdaterResults
            {
                FileAddBytesTarget = jobs.FileAddBulk(),
                FileUpdBytesTarget = jobs.FileUpdBulk()
            };

            string destinationRoot = DestinationRootDir;
            string sourceRoot = SourceRootDir;
            long progressSizeTarget = jobs.TotalBulk();
            int progressNumberTarget = jobs.GrandTotal;
            long progressSizeCounter = 0;
            int progressNumberCounter = 0;

            List<ReplicaAction> actionlistFD = jobs.TaskList("FD");
            List<ReplicaAction> actionlistDD = jobs.TaskList("DD");
            List<ReplicaAction> actionlistDA = jobs.TaskList("DA");
            List<ReplicaAction> actionlistFU = jobs.TaskList("FU");
            List<ReplicaAction> actionlistFA = jobs.TaskList("FA");

            if (actionlistFD.Count > 0)
            {
                foreach (ReplicaAction act in actionlistFD)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;
                    // Remove any attributes from the destination file
                    FileInfo dFile = new System.IO.FileInfo(act.DestinationPath);
                    FileAttributes fileAttribs = dFile.Attributes;

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
                    SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter, progressNumberTarget, results, prog, false);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionlistDD.Count > 0)
            {
                foreach (ReplicaAction act in actionlistDD)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;
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
                    SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter, progressNumberTarget, results, prog, false);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionlistDA.Count > 0)
            {
                foreach (ReplicaAction act in actionlistDA)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;
                    string faute = Kernel.AttemptToAddDirectory(act.DestinationPath);
                    if (string.IsNullOrEmpty(faute))
                    {
                        results.DirAddSuccess++;
                    }
                    else
                    {
                        act.ErrorText = faute;
                        results.DirAddFailure++;
                    }
                    SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter, progressNumberTarget, results, progressChaser, false);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionlistFU.Count > 0)
            {
                foreach (ReplicaAction act in actionlistFU)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;

                    // Remove any ReadOnly attribute from destination file
                    System.IO.FileInfo dFile = new System.IO.FileInfo(act.DestinationPath);
                    System.IO.FileAttributes fileAttribs = dFile.Attributes;
                    if ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        fileAttribs &= ~FileAttributes.ReadOnly; // remove ReadOnly attribute
                        string erreur = Kernel.AttemptToSetFileAttributes(act.DestinationPath, fileAttribs);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText = "Failed to clear file read-only attribute: " + erreur; }
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
                        results.FileUpdBytes += act.Bulk;
                    }
                    else
                    {
                        act.ErrorText = "Failed to update file: " + faute;
                        results.FileUpdFailure++;
                    }
                    SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter, progressNumberTarget, results, progressChaser, false);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionlistFA.Count > 0)
            {
                foreach (ReplicaAction act in actionlistFA)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;

                    // Remove any Archive attribute from source file
                    FileInfo sFile = new System.IO.FileInfo(act.SourcePath(destinationRoot, sourceRoot));
                    FileAttributes fileAttribs = sFile.Attributes;
                    if ((fileAttribs & FileAttributes.Archive) == FileAttributes.Archive)
                    {
                        fileAttribs &= ~FileAttributes.Archive; // remove Archive attribute
                        string erreur = Kernel.AttemptToSetFileAttributes(act.SourcePath(destinationRoot, sourceRoot), fileAttribs);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText = "Failed to clear file archive attribute: " + erreur; }
                    }

                    try
                    {
                        File.Copy(act.SourcePath(destinationRoot, sourceRoot), act.DestinationPath, overwrite: true);
                        results.FileAddSuccess++;
                        results.FileAddBytes += act.Bulk;
                    }
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
                    SendUpdateProgressReport(progressSizeCounter, progressSizeTarget,progressNumberCounter, progressNumberTarget, results, progressChaser, false);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
            }
        finishing:
            // send a final progress report with 'show regardless' flag, to ensure that the final display is up-to-date
            SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter, progressNumberTarget, results, progressChaser, true);
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
            string sd = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask].SourceFoundDrivePath();
            string dd = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask].DestinationFoundDrivePath();

            // NB This implementation polls CancellationToken.IsCancellationRequested (bool) rather than throwing an error
            // Compare the example in FamilyTree - detectIntramarriages which uses the error method (which Cleary favours)
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            await Task.Run(() => ImplementUpdate(sd, dd, tasks, progressChaser, token)).ConfigureAwait(true);
            Kernel.SoundSignal(600, 500);
            PostUpdateDisplay(results);
        }

        private async void Analyse_Click(object sender, RoutedEventArgs e)
        {
            // can be triggered by 'Analyse' button or 'Analyse and Update' button 
            Button source = sender as Button;
            buttonAnalyse.Visibility = Visibility.Collapsed;
            buttonAnalysePlus.Visibility = Visibility.Collapsed;
            if (source == buttonAnalyse) { runOn = false; } else { runOn = true; }

            this.Cursor = Cursors.Wait;
            buttonClose.Visibility = Visibility.Collapsed;
            DisplayMessage("Analysing", waitingInput: false);

            ReplicaAnalysis _analysis = new ReplicaAnalysis(SourceRootDir: boulot.SourceFoundDrivePath(), DestinationRootDir: boulot.DestinationFoundDrivePath(), IncludeHiddenItems: boulot.IncludeHidden, ExpectedItemCount: boulot.ExpectedItemCount, progressChaser);

            await Task.Run(() => _analysis.PerformAnalysis()).ConfigureAwait(true);

            boulot.ExpectedItemCount = _analysis.ExpectedItemCount; // update the expected item count for the job based on this run

            PostAnalysisDisplay(_analysis);
            Kernel.SoundSignal(300, 500);
        }

        void DisplayAnalysisProgress(ProgressInfo i)
        {
            char Q = i.Phase;
            switch (Q)
            {
                case 'S':
                    {
                        progressbarSource.Value = i.PercentNumber;
                        textblockProgressSource.Text =$"{i.PercentNumber}%";
                        break;
                    }
                case 'D':
                    {
                        progressbarDestination.Value =i.PercentNumber;
                        textblockProgressDestination.Text =$"{i.PercentNumber}%";
                        break;
                    }
                case 'U':
                    {
                        progressbarUpdateSize.Value =i.PercentSize;
                        textblockProgressUpdateSize.Text =$"{i.PercentSize}%";
                        progressbarUpdateNumber.Value = i.PercentNumber;
                        textblockProgressUpdateNumber.Text = $"{i.PercentNumber}%";
                        DisplayStatistics(i.Results);
                        if (results.AnyFailures) { SwitchImplementationErrorsDisplay(true); }
                        break;
                    }
            }
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {

            string root = boulot.DestinationFoundDrivePath();
            ActionDetailsWindow w = new ActionDetailsWindow(tasks, root.Length)
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
            lblSourceFiles.FontFamily = fam;
            lblSourceFiles.FontSize = siz;
            lblSourceFolders.FontFamily = fam;
            lblSourceFolders.FontSize = siz;
            lblDestinFiles.FontFamily = fam;
            lblDestinFiles.FontSize = siz;
            lblDestinFolders.FontFamily = fam;
            lblDestinFolders.FontSize = siz;

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
            SetFonts();

            progressbarSource.Value = 0;
            textblockProgressSource.Text = string.Empty;

            progressbarDestination.Value = 0;
            textblockProgressDestination.Text = string.Empty;

            progressbarUpdateSize.Value = 0;
            textblockProgressUpdateSize.Text = string.Empty;
            progressbarUpdateNumber.Value = 0;
            textblockProgressUpdateNumber.Text = string.Empty;
        }

        private void PostAnalysisDisplay(ReplicaAnalysis _anal)
        {
            tasks = _anal.JobTaskBundle;

            lblSourceFolders.Text = _anal.SourceDirectories.ToString("N00", CultureInfo.InvariantCulture);
            lblSourceFolders.Visibility = Visibility.Visible;
            lblSourceFoldersL.Visibility = Visibility.Visible;
            lblSourceFiles.Text = _anal.SourceFiles.ToString("N00", CultureInfo.InvariantCulture);
            lblSourceFiles.Visibility = Visibility.Visible;
            lblSourceFilesL.Visibility = Visibility.Visible;

            lblDestinFolders.Text = _anal.DestinationDirectories.ToString("N00", CultureInfo.InvariantCulture);
            lblDestinFolders.Visibility = Visibility.Visible;
            lblDestinFoldersL.Visibility = Visibility.Visible;
            lblDestinFiles.Text = _anal.DestinationFiles.ToString("N00", CultureInfo.InvariantCulture);
            lblDestinFiles.Visibility = Visibility.Visible;
            lblDestinFilesL.Visibility = Visibility.Visible;

            long bal = _anal.DestinationDirectories - _anal.SourceDirectories;
            if (bal > 0) { lblFolderBalance.Text = bal.ToString("N00", CultureInfo.InvariantCulture) + " more"; } else { if (bal < 0) { lblFolderBalance.Text = Math.Abs(bal).ToString("N00", CultureInfo.InvariantCulture) + " fewer"; } else { lblFolderBalance.Text = string.Empty; } }
            lblFolderBalance.Visibility = Visibility.Visible;

            bal = _anal.DestinationFiles - _anal.SourceFiles;
            if (bal > 0) { lblFileBalance.Text = bal.ToString("N00", CultureInfo.InvariantCulture) + " more"; } else { if (bal < 0) { lblFileBalance.Text = Math.Abs(bal).ToString("N00", CultureInfo.InvariantCulture) + " fewer"; } else { lblFileBalance.Text = string.Empty; } }
            lblFileBalance.Visibility = Visibility.Visible;

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
                runOn = false; // don't automatically run the update if queries have been raised
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
                    if (runOn)
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
            int f = results.FileDelFailure;
            FileDeleteErrorsTBk.Text =(f>0) ? f.ToString(CultureInfo.InvariantCulture) : string.Empty;

            DirectoriesDeletedTBk.Text = results.DirDelSuccess.ToString(CultureInfo.InvariantCulture);
            f = results.DirDelFailure;
            DirectoryDeleteErrorsTBk.Text = (f > 0) ? f.ToString(CultureInfo.InvariantCulture) : string.Empty;

            DirectoriesAddedTBk.Text = results.DirAddSuccess.ToString(CultureInfo.InvariantCulture);
            f = results.DirAddFailure;
            DirectoryAddErrorsTBk.Text = (f > 0) ? f.ToString(CultureInfo.InvariantCulture) : string.Empty;

            FilesUpdatedTBk.Text = results.FileUpdSuccess.ToString(CultureInfo.InvariantCulture);
            f = results.FileUpdFailure;
            FileUpdateErrorsTBk.Text = (f > 0) ? f.ToString(CultureInfo.InvariantCulture) : string.Empty;
            FileBytesToUpdateTBk.Text =Kernel.SizeReport( results.FileUpdBytesTarget);
            FileBytesUpdatedTBk.Text =Kernel.SizeReport( results.FileUpdBytes);

            FilesAddedTBk.Text = results.FileAddSuccess.ToString(CultureInfo.InvariantCulture);
            f = results.FileAddFailure;
            FileAddErrorsTBk.Text = (f > 0) ? f.ToString(CultureInfo.InvariantCulture) : string.Empty; 
            FileBytesToAddTBk.Text =Kernel.SizeReport( results.FileAddBytesTarget);
            FileBytesAddedTBk.Text =Kernel.SizeReport( results.FileAddBytes);
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

            // report destination usage after backup
            long tot =boulot.DestinationSize();
            long fre = boulot.DestinationFree(); ;
            int percentfree = Convert.ToInt32(100 * ((double)fre / tot));
            long freediff = fre - freespacebefore;
            string freediffreport;
            if (freediff <= 0) { freediffreport = Kernel.SizeReport(Math.Abs(freediff)) + " less free space"; } else { freediffreport = Kernel.SizeReport(Math.Abs(freediff)) + " more free space"; }
            lblDestinationAfterScope.Text = $"{Kernel.SizeReport(tot)} total {Kernel.SizeReport(fre)} free ({percentfree}% free) {freediffreport}";
            prgDestinationAfter.Value = 100 - percentfree;
            stackpanelafter.Visibility = Visibility.Visible;

            buttonDetail.Visibility = Visibility.Visible;
            buttonClose.Content = "Finish";
            buttonClose.Visibility = Visibility.Visible;
            buttonClose.Focus();
            this.Cursor = Cursors.Arrow;

            if (results.AnyFailures)
            {
                SwitchImplementationErrorsDisplay(true);
                if (MessageBox.Show("There were errors - do you want to view the action details?", "Replicas", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    string root = boulot.DestinationFoundDrivePath();
                    ActionDetailsWindow w = new ActionDetailsWindow(tasks, root.Length)
                    {
                        Owner = this
                    };
                    w.ShowDialog();
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            buttonCancelUpdate.IsEnabled = false;
            DisplayMessage("Cancel requested...", waitingInput: false);
        }

        private DateTime lastUpdatePRTime=DateTime.Now;

        private void SendUpdateProgressReport(long progressSizeValue, long progressSizeTarget,int progressNumberValue, int progressNumberTarget, UpdaterResults report, IProgress<ProgressInfo> prog, bool showRegardless)
        {
            DateTime ici = DateTime.Now;
            TimeSpan ts = ici - lastUpdatePRTime;
            if ((ts.TotalSeconds < 1) && (!showRegardless)) { return; } // don't send another PR within 1 second unless it is the final one
            lastUpdatePRTime = ici;
            int ProgressReportSize = Convert.ToInt32(((double)progressSizeValue / progressSizeTarget) * 100);
            if (ProgressReportSize > 100) { ProgressReportSize = 100; } // don't exceed 100%
            int ProgressReportNumber = Convert.ToInt32(((double)progressNumberValue / progressNumberTarget) * 100);
            if (ProgressReportNumber > 100) { ProgressReportNumber = 100; } // don't exceed 100%
            ProgressInfo info = new ProgressInfo(ProgressReportSize, ProgressReportNumber, 'U', report);
            prog.Report(info);
        }

        private void SwitchImplementationDisplay(bool flag)
        {
            Visibility vis = Visibility.Hidden;
            if (flag) { vis = Visibility.Visible; }

            ActionsToDoTBk.Visibility = vis;
            ActionsDoneTBk.Visibility = vis;
            BytesToDoTBk.Visibility = vis;
            BytesDoneTBk.Visibility = vis;

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
            FileBytesToUpdateTBk.Visibility = vis;
            FileBytesUpdatedTBk.Visibility = vis;
            FileBytesToAddTBk.Visibility = vis;
            FileBytesAddedTBk.Visibility = vis;
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

        public void Dispose()
        {
                // Explicitly dispose of resources
                Dispose(true);
                // Tell GC [Garbage Collection] not to finalize us--we've already done it manually
                GC.SuppressFinalize(this);
        }

        // Function called via Dispose method or via Finalizer
        protected virtual void Dispose(bool explicitDispose)
        {
            if (!disposed)
            {
                // Free some resources only when invoking via Dispose
                if (!(cts is null))
                {
                    if (explicitDispose) { cts.Dispose(); }
                }
                
                    //FreeManagedResources();   // Define this method

                // Free unmanaged resources here--whether via Dispose
                //   or via finalizer
                //FreeUnmanagedResources();

                disposed = true;
            }
        }
        // Finalizer
        ~RunWindow()
        {
            Dispose(false);
        }
}