using System;
using System.Collections.Generic;
using System.Linq;

namespace Replicas;

internal class DetectedDrives
{
    // NOTE Data is runtime only

        private class FoundDisk
        {
            public char? DriveLetter;
            public string VolumeLabel;
            public string RootDir;
            public long Size;
            public int UsedPercent;

            public FoundDisk()
            {
                DriveLetter = null;
                VolumeLabel = string.Empty;
                RootDir = string.Empty;
            }
        }

        private readonly List<FoundDisk> _trove = new List<FoundDisk>();

        public DetectedDrives() // constructor
        { 
            Refresh();
        }

        public string DriveLetters
        {
            get
            {
                string lets = string.Empty;
                foreach (FoundDisk fd in _trove)
                {
                    if (fd.DriveLetter.HasValue)
                    {
                        lets += fd.DriveLetter.Value;
                    }
                }
                return lets;
            }
        }

        private void Refresh()
        {
            _trove.Clear();
            System.IO.DriveInfo[] foundDrives = System.IO.DriveInfo.GetDrives();
            foreach (System.IO.DriveInfo drv in foundDrives)
            {
                if (!drv.Name.StartsWith("A",StringComparison.OrdinalIgnoreCase))
                {
                    if (drv.DriveType != System.IO.DriveType.CDRom)
                    {
                        if (drv.IsReady)
                        {
                            string rd = drv.RootDirectory.FullName;
                            var dv = new FoundDisk()
                            {
                                DriveLetter = drv.Name.ElementAt(0),
                                VolumeLabel = drv.VolumeLabel,
                                RootDir = rd,
                                Size = drv.TotalSize,
                                UsedPercent = (int)Math.Round((drv.TotalSize - drv.AvailableFreeSpace) / (double)drv.TotalSize * 100)
                            };
                            _trove.Add(dv);
                        }
                    }
                }
            }
            // 2017 May 17 add drive id by text file for drives lacking a volume label
            foreach (FoundDisk l in _trove)
            {
                if (string.IsNullOrWhiteSpace(l.VolumeLabel))
                {
                    string path = l.RootDir;
                    path = System.IO.Path.Combine(path, "jbhDriveId.txt");
                    if (System.IO.File.Exists(path))
                    {
                        using var sr = new System.IO.StreamReader(path);
                        string? read = sr.ReadLine();
                        if (read is { })
                        {
                            l.VolumeLabel = read;    
                        }
                    }
                    if (string.IsNullOrWhiteSpace(l.VolumeLabel)) // no predefined label
                    {
                        // create a new label
                        int j = 1;
                        string lbl = $"jbhVol{j}";
                        while (IsExistingLabel(lbl))
                        {
                            j++;
                            lbl = $"jbhVol{j}";
                        }
                        try
                        {
                            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                            {
                                sw.WriteLine(lbl);
                            }
                            l.VolumeLabel = lbl;
                        }
                        catch (UnauthorizedAccessException)
                        {

                        }
                        catch (System.IO.IOException)
                        {

                        }
                    }
                }
            }
        }

        public char? DriveLetter(string driveLabel, string drivePath)
        {
            // Use volume label to identify drive letter when possible, but for case in which volume label is empty, use drive letter from path
            if (string.IsNullOrWhiteSpace(driveLabel))
            { return drivePath.ElementAt(0); }
            else
            {
                char? l = null;
                foreach (FoundDisk t in _trove)
                { if (t.VolumeLabel == driveLabel) { l = t.DriveLetter; } }
                return l;
            }
        }

        public char? DriveLetter(string driveLabel)
        {
            // Use volume label to identify drive letter when possible else return null
            if (string.IsNullOrWhiteSpace(driveLabel))
            { return null; }
            else
            {
                char? l = null;
                foreach (FoundDisk t in _trove)
                { if (t.VolumeLabel == driveLabel) { l = t.DriveLetter; } }
                return l;
            }
        }

        public string DriveVolumeLabel(char? driveLetter)
        {
            if (driveLetter == null)
            { return string.Empty; }
            else
            {
                string l = string.Empty;
                foreach (FoundDisk t in _trove)
                { if (t.DriveLetter == driveLetter) { l = t.VolumeLabel; } }
                return l;
            }
        }

        public long DriveVolumeSize(char? driveLetter)
        {
            if (driveLetter == null)
            { return 0; }
            else
            {
                long l = 0;
                foreach (FoundDisk t in _trove)
                { if (t.DriveLetter == driveLetter) { l = t.Size; } }
                return l;
            }
        }

        public int DriveVolumeUsedPercent(char? driveLetter)
        {
            if (driveLetter == null)
            { return 0; }
            else
            {
                int u = 0;
                foreach (FoundDisk t in _trove)
                { if (t.DriveLetter == driveLetter) { u = t.UsedPercent; } }
                return u;
            }
        }

        private bool IsExistingLabel(string test)
        {
            bool rv = false;
            foreach (FoundDisk l in _trove)
            {
                if (l.VolumeLabel.Equals(test, StringComparison.OrdinalIgnoreCase)) { rv = true; break; }
            }
            return rv;
        }
}