using System;
using System.IO;

namespace Replicas;

internal class FoundItem
{
    public bool IsDirectory { get; set; } // otherwise is file
        public string PathTail { get; set; }
        private string SourcePath { get; set; }
        private string DestinPath { get; set; }
        public string DestinationPath { get { return DestinPath; } }
        public bool SourcePresent { get; set; }
        public bool DestinPresent { get; set; }
        public long FileAgeDifferenceSeconds { get; set; } // 1 = source newer; -1 = destin newer ; 0 = same
        public bool FileSizeDifference { get; set; }

        public long TaskBulk { get; set; }

        public FoundItem(string fullPath, bool isDir, bool isSource, string sRoot, string dRoot)
        { // Constructor
            if (string.IsNullOrWhiteSpace(fullPath)) { throw new ArgumentNullException(paramName: nameof(fullPath)); }
            if (string.IsNullOrWhiteSpace(sRoot)) { throw new ArgumentNullException(paramName: nameof(sRoot)); }
            if (string.IsNullOrWhiteSpace(dRoot)) { throw new ArgumentNullException(paramName: nameof(dRoot)); }
            IsDirectory = isDir;
            TaskBulk = 100;// The notional task size when not adding or updating a file [ie it's a directory action or a file deletion]
            if (isSource)
            {
                PathTail = fullPath.Substring(sRoot.Length);
                SourcePath = fullPath;
                DestinPath = dRoot + PathTail;
                SourcePresent = true;
                if (isDir)
                {
                    DestinPresent = Directory.Exists(DestinPath);
                }
                else
                {
                    FileInfo sfi = new FileInfo(SourcePath);
                    TaskBulk = sfi.Length;
                    DestinPresent = File.Exists(DestinPath);
                    if (DestinPresent)
                    {
                        FileInfo dfi = new FileInfo(DestinPath);
                        FileSizeDifference = (sfi.Length != dfi.Length);
                        FileAgeDifferenceSeconds = 0;
                        TimeSpan ts = (sfi.LastWriteTimeUtc - dfi.LastWriteTimeUtc);
                        double seks = ts.TotalSeconds;
                        FileAgeDifferenceSeconds = (long)seks;
                        double seksAbs = Math.Abs(seks); // absolute time difference
                        if (seksAbs < 60) { FileAgeDifferenceSeconds = 0; } //   60 =  number of seconds = the time difference between the ages of files below which the difference will be ignored.
                    }
                }
            }
            else
            {
                // is destination, not source
                PathTail = fullPath.Substring(dRoot.Length);
                SourcePath = sRoot + PathTail;
                DestinPath = fullPath;
                DestinPresent = true;
                SourcePresent = false; // assumed because it would have been picked up in the prior Source scan
            }
        }
}