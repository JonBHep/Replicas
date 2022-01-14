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

internal partial class RunWindow
{

    private readonly Tache _boulot;
    private ReplicaJobTasks? _tasks;
    private UpdaterResults? _results;
    private bool _includeHidden;
    private bool _runOn; // whether to run straight on from analysis to perform the update
    private long _freeSpaceBefore;
    internal bool Fulfilled;
    private Progress<ProgressInfo>? _progressChaser;
    private CancellationTokenSource? _cts;

    internal RunWindow()
    {
        InitializeComponent();
        _boulot = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask];
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
            
            txtTitle.Text = $"Replicas - {_boulot.JobTitle}";

            _includeHidden =_boulot.IncludeHidden;
            lblPathS.Text =_boulot.SourceFoundDrivePath();
            lblPathD.Text =_boulot.DestinationFoundDrivePath();
            string v =_boulot.SourceVolume;
            lblDriveS.Text = Kernel.Instance.KnownDrives.VolumeDescription(v) + " (" + v + ")";
            v =_boulot.DestinationVolume;
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

            lblLastPerformed.Text =_boulot.PeriodSinceLastRun();
            lblHidden.Text = _includeHidden ? "Included" : "Not included";

            // report source and destination usage before backup
            long tot = _boulot.SourceSize();
            long fre =_boulot.SourceFree();
            int percentFree = Convert.ToInt32(100 * ((double)fre / tot));
            lblSourceScope.Text = $"{Kernel.SizeReport(tot)} total {Kernel.SizeReport(fre)} free ({percentFree}% free)";
            prgSource.Value = 100 - percentFree;

            tot =_boulot.DestinationSize();
            fre =_boulot.DestinationFree();
            _freeSpaceBefore = fre;
            percentFree = Convert.ToInt32(100 * ((double)fre / tot));
            lblDestinationAfterScope.Text = lblDestinationBeforeScope.Text = $"{Kernel.SizeReport(tot)} total {Kernel.SizeReport(fre)} free ({percentFree}% free)";
            prgDestinationAfter.Value = prgDestinationBefore.Value = 100 - percentFree;

            buttonClose.Visibility = Visibility.Visible;
            buttonClose.Content = "Cancel";
            buttonCancelUpdate.Visibility = Visibility.Collapsed;

            SwitchImplementationDisplay(false);
            SwitchImplementationErrorsDisplay(false); 

            _progressChaser = new Progress<ProgressInfo>(DisplayAnalysisProgress);
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
            if (buttonClose.Visibility == Visibility.Visible) return;
            MessageBox.Show("JBH: I am not allowing window closure because the Close button is not visible", "Replicas", MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Cancel = true;
        }

        private void ImplementUpdate(string sourceRootDir, string destinationRootDir, ReplicaJobTasks jobs, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
        {
            _results = new UpdaterResults
            {
                FileAddBytesTarget = jobs.FileAddBulk(),
                FileUpdBytesTarget = jobs.FileUpdBulk()
            };

            string destinationRoot = destinationRootDir;
            string sourceRoot = sourceRootDir;
            long progressSizeTarget = jobs.TotalBulk();
            int progressNumberTarget = jobs.GrandTotal;
            long progressSizeCounter = 0;
            int progressNumberCounter = 0;

            List<ReplicaAction> actionListFd = jobs.TaskList("FD");
            List<ReplicaAction> actionListDd = jobs.TaskList("DD");
            List<ReplicaAction> actionListDa = jobs.TaskList("DA");
            List<ReplicaAction> actionListFu = jobs.TaskList("FU");
            List<ReplicaAction> actionListFa = jobs.TaskList("FA");

            if (actionListFd.Count > 0)
            {
                foreach (ReplicaAction act in actionListFd)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;
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
                    SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter, progressNumberTarget, _results, progress, false);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionListDd.Count > 0)
            {
                foreach (ReplicaAction act in actionListDd)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;
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
                    SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter, progressNumberTarget, _results, progress, false);
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionListDa.Count > 0)
            {
                foreach (ReplicaAction act in actionListDa)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;
                    string faute = Kernel.AttemptToAddDirectory(act.DestinationPath);
                    if (string.IsNullOrEmpty(faute))
                    {
                        _results.DirAddSuccess++;
                    }
                    else
                    {
                        act.ErrorText = faute;
                        _results.DirAddFailure++;
                    }

                    if (_progressChaser != null)
                    {
                        SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter
                            , progressNumberTarget, _results, _progressChaser, false);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                    
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionListFu.Count > 0)
            {
                foreach (ReplicaAction act in actionListFu)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;

                    // Remove any ReadOnly attribute from destination file
                    FileInfo dFile = new FileInfo(act.DestinationPath);
                    FileAttributes fileAttribs = dFile.Attributes;
                    if ((fileAttribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        fileAttribs &= ~FileAttributes.ReadOnly; // remove ReadOnly attribute
                        string erreur = Kernel.AttemptToSetFileAttributes(act.DestinationPath, fileAttribs);
                        if (!string.IsNullOrEmpty(erreur)) { act.ErrorText = "Failed to clear file read-only attribute: " + erreur; }
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
                        _results.FileUpdBytes += act.Bulk;
                    }
                    else
                    {
                        act.ErrorText = "Failed to update file: " + faute;
                        _results.FileUpdFailure++;
                    }

                    if (_progressChaser != null)
                    {
                        SendUpdateProgressReport(progressSizeCounter, progressSizeTarget
                            , progressNumberCounter, progressNumberTarget, _results, _progressChaser, false);    
                    }
                    
                    if (cancellationToken.IsCancellationRequested) { break; }
                }
                if (cancellationToken.IsCancellationRequested) { goto finishing; }
            }

            if (actionListFa.Count > 0)
            {
                foreach (ReplicaAction act in actionListFa)
                {
                    progressSizeCounter += act.Bulk;
                    progressNumberCounter++;

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
                    {
                        File.Copy(act.SourcePath(destinationRoot, sourceRoot), act.DestinationPath, overwrite: true);
                        _results.FileAddSuccess++;
                        _results.FileAddBytes += act.Bulk;
                    }
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

                    if (_progressChaser != null)
                    {
                        SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter
                            , progressNumberTarget, _results, _progressChaser, false);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
            }
        finishing:
            // send a final progress report with 'show regardless' flag, to ensure that the final display is up-to-date
            if (_progressChaser != null)
            {
                SendUpdateProgressReport(progressSizeCounter, progressSizeTarget, progressNumberCounter
                    , progressNumberTarget, _results, _progressChaser, true);
                if (cancellationToken.IsCancellationRequested)
                {
                    _results.WasCancelled = true;
                }
            }
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
            Cursor = Cursors.Wait;
            string sd = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask].SourceFoundDrivePath();
            string dd = Kernel.Instance.JobProfiles.Jobs[Kernel.Instance.CurrentTask].DestinationFoundDrivePath();

            // NB This implementation polls CancellationToken.IsCancellationRequested (bool) rather than throwing an error
            // Compare the example in FamilyTree - detectIntramarriages which uses the error method (which Cleary favours)
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            if (_tasks != null)
            {
                if (_progressChaser != null)
                {
                    await Task.Run(() => ImplementUpdate(sd, dd, _tasks, _progressChaser, token)).ConfigureAwait(true);        
                }
            }
            Kernel.SoundSignal(600, 500);
            if (_results != null)
            {
                PostUpdateDisplay(_results);
            }
        }

        private async void Analyse_Click(object sender, RoutedEventArgs e)
        {
            // can be triggered by 'Analyse' button or 'Analyse and Update' button 
            Button? source = sender as Button;
            buttonAnalyse.Visibility = Visibility.Collapsed;
            buttonAnalysePlus.Visibility = Visibility.Collapsed;
            _runOn = source != buttonAnalyse;

            Cursor = Cursors.Wait;
            buttonClose.Visibility = Visibility.Collapsed;
            DisplayMessage("Analysing", waitingInput: false);
            if (_progressChaser != null)
            {
                ReplicaAnalysis analysis = new ReplicaAnalysis(SourceRootDir: _boulot.SourceFoundDrivePath()
                    , DestinationRootDir: _boulot.DestinationFoundDrivePath(), IncludeHiddenItems: _boulot.IncludeHidden
                    , ExpectedItemCount: _boulot.ExpectedItemCount, _progressChaser);
                await Task.Run(() => analysis.PerformAnalysis()).ConfigureAwait(true);

                _boulot.ExpectedItemCount
                    = analysis.ExpectedItemCount; // update the expected item count for the job based on this run

                PostAnalysisDisplay(analysis);
            }

            Kernel.SoundSignal(300, 500);
        }

        void DisplayAnalysisProgress(ProgressInfo i)
        {
            char q = i.Phase;
            switch (q)
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
                        if (_results is {AnyFailures: true}) { SwitchImplementationErrorsDisplay(true); }
                        break;
                    }
            }
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            string root = _boulot.DestinationFoundDrivePath();
            if (_tasks != null)
            {
                ActionDetailsWindow w = new ActionDetailsWindow(_tasks, root.Length)
                {
                    Owner = this
                };
                w.ShowDialog();
            }
        }

        private void DisplayMessage(string phase, bool waitingInput)
        {
            textblockMessage.Foreground = waitingInput ? Brushes.DarkRed : Brushes.ForestGreen;
            textblockMessage.Text = phase;
        }

        private void SetFonts()
        {
            // sets fixed digit width font for numeric displays
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

        private void PostAnalysisDisplay(ReplicaAnalysis replicaAnalysis)
        {
            _tasks = replicaAnalysis.JobTaskBundle;

            lblSourceFolders.Text = replicaAnalysis.SourceDirectories.ToString("N00", CultureInfo.InvariantCulture);
            lblSourceFolders.Visibility = Visibility.Visible;
            lblSourceFoldersL.Visibility = Visibility.Visible;
            lblSourceFiles.Text = replicaAnalysis.SourceFiles.ToString("N00", CultureInfo.InvariantCulture);
            lblSourceFiles.Visibility = Visibility.Visible;
            lblSourceFilesL.Visibility = Visibility.Visible;

            lblDestinFolders.Text = replicaAnalysis.DestinationDirectories.ToString("N00", CultureInfo.InvariantCulture);
            lblDestinFolders.Visibility = Visibility.Visible;
            lblDestinFoldersL.Visibility = Visibility.Visible;
            lblDestinFiles.Text = replicaAnalysis.DestinationFiles.ToString("N00", CultureInfo.InvariantCulture);
            lblDestinFiles.Visibility = Visibility.Visible;
            lblDestinFilesL.Visibility = Visibility.Visible;

            long bal = replicaAnalysis.DestinationDirectories - replicaAnalysis.SourceDirectories;
            lblFolderBalance.Text = bal switch
            {
                > 0 => $"{bal:N00} more" 
                , < 0 => $"{Math.Abs(bal):N00} fewer"
                , _ => string.Empty
            };
            lblFolderBalance.Visibility = Visibility.Visible;

            bal = replicaAnalysis.DestinationFiles - replicaAnalysis.SourceFiles;
            lblFileBalance.Text = bal switch
            {
                > 0 => $"{bal:N00} more" 
                , < 0 => $"{Math.Abs(bal):N00} fewer"
                , _ => string.Empty
            };
            lblFileBalance.Visibility = Visibility.Visible;

            FilesToAddTBk.Text = replicaAnalysis.JobTaskBundle.TaskCount("FA").ToString(CultureInfo.InvariantCulture);
            DirectoriesToAddTBk.Text = replicaAnalysis.JobTaskBundle.TaskCount("DA").ToString(CultureInfo.InvariantCulture);
            FilesToDeleteTBk.Text = replicaAnalysis.JobTaskBundle.TaskCount("FD").ToString(CultureInfo.InvariantCulture);
            DirectoriesToDeleteTBk.Text = replicaAnalysis.JobTaskBundle.TaskCount("DD").ToString(CultureInfo.InvariantCulture);
            FilesToUpdateTBk.Text = replicaAnalysis.JobTaskBundle.TaskCount("FU").ToString(CultureInfo.InvariantCulture);

            SwitchImplementationDisplay(true);

            Cursor = Cursors.Arrow;

            var f = "Some source files are older than the corresponding destination files.\nThis may mean that the destination directory has been updated more recently than the source directory.\nIt could also result from files being renamed.\nAll files of different date or size will be overwritten by source files.\nDo not update unless confident that you want to replace newer files with older.";
            if (replicaAnalysis.OlderSourceWarnings.Count > 0)
            {
                WarningsWindow ww = new WarningsWindow();
                ww.SetCaption("Source older than destination");
                ww.SetComment(f);
                ww.LstExamples.Items.Clear();
                foreach (string g in replicaAnalysis.OlderSourceWarnings) { ww.LstExamples.Items.Add(g); }
                ww.ShowDialog();
                _runOn = false; // don't automatically run the update if queries have been raised
            }

            if (replicaAnalysis.JobTaskBundle.GrandTotal > 0)
            {
                if (replicaAnalysis.JobTaskBundle.PathAndFilenameLengthsOk())
                {
                    DisplayMessage("Ready to update", waitingInput: true);
                    buttonUpdate.IsEnabled = true;
                    buttonUpdate.Visibility = Visibility.Visible;
                    buttonDetail.Tag = replicaAnalysis.JobTaskBundle;
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
            long tot =_boulot.DestinationSize();
            long fre = _boulot.DestinationFree(); 
            int percentFree = Convert.ToInt32(100 * ((double)fre / tot));
            long freeDifference = fre - _freeSpaceBefore;
            string freeDiffReport;
            if (freeDifference <= 0)
            {
                freeDiffReport = Kernel.SizeReport(Math.Abs(freeDifference)) + " less free space";
            }
            else
            {
                freeDiffReport = Kernel.SizeReport(freeDifference) + " more free space";
            }

            lblDestinationAfterScope.Text = $"{Kernel.SizeReport(tot)} total {Kernel.SizeReport(fre)} free ({percentFree}% free) {freeDiffReport}";
            prgDestinationAfter.Value = 100 - percentFree;
            stackpanelafter.Visibility = Visibility.Visible;

            buttonDetail.Visibility = Visibility.Visible;
            buttonClose.Content = "Finish";
            buttonClose.Visibility = Visibility.Visible;
            buttonClose.Focus();
            Cursor = Cursors.Arrow;

            if (results.AnyFailures)
            {
                SwitchImplementationErrorsDisplay(true);
                if (MessageBox.Show("There were errors - do you want to view the action details?", "Replicas", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    string root = _boulot.DestinationFoundDrivePath();
                    if (_tasks != null)
                    {
                        ActionDetailsWindow w = new ActionDetailsWindow(_tasks, root.Length)
                        {
                            Owner = this
                        };
                        w.ShowDialog();
                    }
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            buttonCancelUpdate.IsEnabled = false;
            DisplayMessage("Cancel requested...", waitingInput: false);
        }

        private DateTime _lastUpdatePrTime=DateTime.Now;

        private void SendUpdateProgressReport(long progressSizeValue, long progressSizeTarget,int progressNumberValue, int progressNumberTarget, UpdaterResults report, IProgress<ProgressInfo> progress, bool showRegardless)
        {
            DateTime ici = DateTime.Now;
            TimeSpan ts = ici - _lastUpdatePrTime;

            if ((ts.TotalSeconds < 1) && (!showRegardless)) { return; } // don't send another PR within 1 second unless it is the final one
            _lastUpdatePrTime = ici;
            int progressReportSize = Convert.ToInt32(((double)progressSizeValue / progressSizeTarget) * 100);
            if (progressReportSize > 100) { progressReportSize = 100; } // don't exceed 100%
            int progressReportNumber = Convert.ToInt32(((double)progressNumberValue / progressNumberTarget) * 100);
            if (progressReportNumber > 100) { progressReportNumber = 100; } // don't exceed 100%
            ProgressInfo info = new ProgressInfo(progressReportSize, progressReportNumber, 'U', report);
            progress.Report(info);
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

}