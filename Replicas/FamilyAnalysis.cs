using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Replicas;

internal class FamilyAnalysis
{
    private readonly string _sourceRoot;
        private readonly string _destinationRoot;
        private readonly Hashtable _foundDestinationItemsIndex = new Hashtable();
        private readonly List<FoundItem> _finds = new List<FoundItem>();
        private readonly List<FoundItem> _problematics = new List<FoundItem>();
        private long _runningItemCount;
        private int _lastProgressReport = 100;
        private readonly IProgress<ProgressInfo> _progressor;

        public FamilyAnalysis(string sourceRootDir, string destinationRootDir, long expectedItemCount, Progress<ProgressInfo> prog)//constructor
        {
            DestinationDirectories = 0;
            DestinationFiles = 0;
            SourceDirectories = 0;
            SourceFiles = 0;
            _sourceRoot = sourceRootDir;
            _destinationRoot = destinationRootDir;
            this.ExpectedItemCount = expectedItemCount;
            if (this.ExpectedItemCount == 0) { this.ExpectedItemCount = 100; }
            JobTaskBundle = new ReplicaJobTasks();
            _progressor = prog;
        }

        internal int DestinationDirectories { get; private set; }
        internal int DestinationFiles { get; private set; }
        internal int SourceDirectories { get; private set; }
        internal int SourceFiles { get; private set; }
        internal ReplicaJobTasks JobTaskBundle { get; private set; }
        internal List<string> OlderSourceWarnings { get; } = new List<string>();
        internal long ExpectedItemCount { get; private set; }

        internal void PerformAnalysis()
        {
            _finds.Clear();
            _foundDestinationItemsIndex.Clear();
            _runningItemCount = 0;
            BranchSource(_sourceRoot);

            SendProgressReport(ExpectedItemCount, ExpectedItemCount, 'S', _progressor); // ensure that when source scan finishes, progressbar shows 100%

            ExpectedItemCount = _runningItemCount;
            _runningItemCount = 0;
            BranchDestination(_destinationRoot);
            SendProgressReport(ExpectedItemCount, ExpectedItemCount, 'D', _progressor); // ensure that when destination scan finishes, progressbar shows 100%

            ThinActionItems();
            SelectActionItems();
        }

        private void BranchDestination(string branchRoot)
        {
            string[] subfiles;
            string[] subfolders;
            subfiles = Directory.GetFiles(branchRoot);
            subfolders = Directory.GetDirectories(branchRoot);
            foreach (string thing in subfiles)
            {
                DestinationFiles++;
                _runningItemCount++;
                SendProgressReport(_runningItemCount, ExpectedItemCount, 'D', _progressor);
                if (!_foundDestinationItemsIndex.ContainsKey(thing)) // only consider this file / directory if it was not picked up on the source scan
                {
                    FoundItem newItem = new FoundItem(fullPath: thing, isDir: false, isSource: false, sRoot: _sourceRoot, dRoot: _destinationRoot);
                    _finds.Add(newItem);
                    // no need to add to hashtable as we shall not be considering this item again
                }
            }
            foreach (string thing in subfolders)
            {
                DestinationDirectories++;
                _runningItemCount++;
                SendProgressReport(_runningItemCount, ExpectedItemCount, 'D', _progressor);
                if (!_foundDestinationItemsIndex.ContainsKey(thing)) // only consider this file / directory if it was not picked up on the source scan
                {
                    FoundItem newItem = new FoundItem(fullPath: thing, isDir: true, isSource: false, sRoot: _sourceRoot, dRoot: _destinationRoot);
                    _finds.Add(newItem);
                    // no need to add to hashtable as we shall not be considering this item again
                }
                BranchDestination(thing);
            }
        }

        private bool BranchSource(string branchRoot)
        {
            string[] subfiles;
            string[] subfolders;

            subfiles = Kernel.AttemptToGetFiles(branchRoot, out string faute);
            if (!string.IsNullOrEmpty(faute))
            { 
                System.Windows.MessageBox.Show(faute + "\nSkipping corrupted or protected directory\n" + branchRoot,Jbh.AppManager.AppName+ " (ScanFolder)", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Asterisk); return false; 
            }

            bool hasContents = false;

            subfolders = Directory.GetDirectories(branchRoot);
            // add files to source list unless they are hidden and includeHidden is false
            foreach (string thing in subfiles)
            {
                if (NotHiding(filespec: thing, isdirectory: false))
                {
                    hasContents = true;
                    SourceFiles++;
                    _runningItemCount++;
                    SendProgressReport(_runningItemCount, ExpectedItemCount, 'S', _progressor);
                    FoundItem newItem = new FoundItem(fullPath: thing, isDir: false, isSource: true, sRoot: _sourceRoot, dRoot: _destinationRoot);
                    _finds.Add(newItem);
                    _foundDestinationItemsIndex.Add(newItem.DestinationPath, newItem.DestinationPath); // add destination path to hashtable (regardless of whether the destination file exists)
                }
            }
            // add directories to source list unless they are hidden and includeHidden is false
            foreach (string thing in subfolders)
            {
                if (NotHiding(filespec: thing, isdirectory: true))
                {
                    if (BranchSource(thing))
                    {
                        // only record this folder if it has contents (not counting a Private subfolder)
                        hasContents = true;
                        SourceDirectories++;
                        _runningItemCount++;

                        SendProgressReport(_runningItemCount, ExpectedItemCount, 'S', _progressor);
                        FoundItem newItem = new FoundItem(fullPath: thing, isDir: true, isSource: true, sRoot: _sourceRoot, dRoot: _destinationRoot);
                        _finds.Add(newItem);
                        _foundDestinationItemsIndex.Add(newItem.DestinationPath, newItem.DestinationPath); // add destination path to hashtable (regardless of whether the destination directory exists)
                    }
                }
            }

            return hasContents;
        }

        private static bool NotHiding(string filespec, bool isdirectory)
        {
            // whether to include file or directory based on 'Hidden' attribute and whether the dir name = Private
            if (isdirectory)
            {
                string nom = Path.GetFileName(filespec);
                if (nom == "Private") { return false; }
            }
            FileAttributes myAttrib;
            if (isdirectory)
            {
                myAttrib = new DirectoryInfo(filespec).Attributes;
            }
            else
            {
                myAttrib = new FileInfo(filespec).Attributes;
            }

            if (!((myAttrib & FileAttributes.Hidden) == FileAttributes.Hidden)) { return true; }
            // remaining possibility: file or directory is 'Hidden' and option to include hidden files is not selected
            return false;
        }

        private void ThinActionItems()
        { // REDUCE THE LIST OF FOUND ITEMS TO THOSE REQUIRING ACTION
            bool problem;
            //int count = 0;
            foreach (FoundItem fi in _finds)
            {
                problem = false;
                if (fi.DestinPresent && !fi.SourcePresent)
                { problem = true; } // item needs deleting
                else if (fi.SourcePresent && !fi.DestinPresent)
                { problem = true; } // item needs adding
                else if (!fi.IsDirectory) // item is a file (not a directory) and both source and destination are present
                {
                    if (fi.FileSizeDifference)
                    { problem = true; }
                    else if (fi.FileAgeDifferenceSeconds != 0)
                    { problem = true; }
                }
                if (problem) { _problematics.Add(fi); }
            }
        }

        private void SelectActionItems()
        {
            JobTaskBundle = new ReplicaJobTasks();

            for (int n = 0; n < _problematics.Count; n++)
            {
                if (_problematics[n].DestinPresent && !_problematics[n].SourcePresent)
                {
                    if (_problematics[n].IsDirectory)
                    {
                        string d = _problematics[n].DestinationPath;
                        // Delete directory
                        JobTaskBundle.AddTask("DD", d, "Orphan", _problematics[n].TaskBulk);
                    }
                    else
                    {
                        string d = _problematics[n].DestinationPath;
                        // Delete file
                        JobTaskBundle.AddTask("FD", d, "Orphan", _problematics[n].TaskBulk);
                    }
                }
            }
            for (int n = 0; n < _problematics.Count; n++)
            {
                if (_problematics[n].IsDirectory)
                {
                    if (_problematics[n].SourcePresent && !_problematics[n].DestinPresent)
                    {
                        string d = _problematics[n].DestinationPath;
                        // Add directory
                        JobTaskBundle.AddTask("DA", d, "Absent", _problematics[n].TaskBulk);
                    }
                }
                else // target is a file, not a directory
                {
                    if (_problematics[n].SourcePresent && _problematics[n].DestinPresent)
                    {
                        if (_problematics[n].FileAgeDifferenceSeconds != 0) // check first for file date, to ensure that all OLDER SOURCE items are trapped and notified to the user
                        {
                            long seks = _problematics[n].FileAgeDifferenceSeconds;
                            if (seks > 0)
                            {
                                string d = _problematics[n].DestinationPath;
                                // Update file
                                JobTaskBundle.AddTask("FU", d, "Source file newer by " + Kernel.SayTime(seks), _problematics[n].TaskBulk);
                            }
                            else
                            {
                                string d = _problematics[n].DestinationPath;
                                // Update file
                                JobTaskBundle.AddTask("FU", d, "Source file OLDER by " + Kernel.SayTime(seks), _problematics[n].TaskBulk);
                                OlderSourceWarnings.Add(d);
                            }
                        }
                        else if (_problematics[n].FileSizeDifference)
                        {
                            string d = _problematics[n].DestinationPath;
                            // Update file
                            JobTaskBundle.AddTask("FU", d, "File size discrepancy", _problematics[n].TaskBulk);
                        }
                    }
                    else if (_problematics[n].SourcePresent && !_problematics[n].DestinPresent)
                    {
                        string d = _problematics[n].DestinationPath;
                        // Add file
                        JobTaskBundle.AddTask("FA", d, "Absent", _problematics[n].TaskBulk);
                    }
                }
            }
        }
        
        private void SendProgressReport(long progressValue, long progressTarget, char phase, IProgress<ProgressInfo> progress)
        {
            if (progressTarget == 0) { progressTarget = 100; }// avoid overflow errors
            int progressReport = Convert.ToInt32(((double)progressValue / progressTarget) * 100);
            if (progressReport > 100) { progressReport = 100; } // don't exceed 100%
            if (progressReport != _lastProgressReport)
            {
                var info = new ProgressInfo(0, progressReport, phase, null);
                progress.Report(info);
                _lastProgressReport = progressReport;
            }
        }
}