using System;
using System.Collections.Generic;
using System.Globalization;

namespace Replicas;

internal class RememberedDrives
{
    internal readonly List<RememberedDrive> RecognisedDrives = new List<RememberedDrive>();

        private readonly string _drivesDataFile = System.IO.Path.Combine(Jbh.AppManager.DataPath, "Drives.jbhd");

        internal RememberedDrives()
        {
            if (System.IO.File.Exists(_drivesDataFile))
            {
                using System.IO.StreamReader sr = new System.IO.StreamReader(_drivesDataFile);
                RecognisedDrives.Clear();
                while (!sr.EndOfStream)
                {
                    string? read = sr.ReadLine();
                    if (read is { })
                    {
                        RecognisedDrives.Add(new RememberedDrive() {Specification = read});
                    }
                }
                RecognisedDrives.Sort();
            }
        }

        internal void GetJobReferences()
        {
            // runtime (dynamic) update of number of jobs referencing each drive
            for (int a = 0; a < RecognisedDrives.Count; a++)
            {
                int jobs = 0;
                foreach(string clef in Kernel.Instance.JobProfiles.Jobs.Keys)
                {
                    if ((Kernel.Instance.JobProfiles.Jobs[clef].SourceVolume == Kernel.Instance.KnownDrives.RecognisedDrives[a].VolumeLabel) || (Kernel.Instance.JobProfiles.Jobs[clef].DestinationVolume == Kernel.Instance.KnownDrives.RecognisedDrives[a].VolumeLabel))
                    {
                        jobs++;
                    }
                }
                RecognisedDrives[a].JobReferences = jobs;
            }
        }

        internal void SaveProfile()
        {
            Jbh.AppManager.CreateBackupDataFile(_drivesDataFile);
            Jbh.AppManager.PurgeOldBackups("jbhd", 20, 20);
            using var sw = new System.IO.StreamWriter(_drivesDataFile);
            foreach (var kl in RecognisedDrives)
            {
                sw.WriteLine(kl.Specification);
            }
        }

        private int RememberedDriveIndex(string label)
        {
            int rv = -1;
            int n = -1;
            foreach (RememberedDrive kl in RecognisedDrives)
            {
                n++;
                if (kl.VolumeLabel == label) { rv = n; break; }
            }
            return rv;
        }

        internal void SpecifyVolumeDescription(string driveLabel, string value)
        {
            int n = RememberedDriveIndex(driveLabel);

            if (n >= 0)
            {
                RecognisedDrives[n].MyDescription=value;
                RecognisedDrives.Sort();
            }
        }

        internal void KillVolume(string driveLabel)
        {
            int n = RememberedDriveIndex(driveLabel);

            if (n >= 0)
            {
                RecognisedDrives.RemoveAt(n);
            }
        }

        internal string LastConnected(int index)
        {
            if (RecognisedDrives[index].LastConnected == 0) { return "N/K"; }
            DateTime when=DateTime.FromBinary(RecognisedDrives[index].LastConnected);
            return when.ToString("ddd dd MMM yyyy @ HH:mm", CultureInfo.CurrentCulture);
        }

        internal string VolumeDescription(string? driveLabel)
        {
            if (driveLabel is { })
            {
                int n = RememberedDriveIndex(driveLabel);
                if (n >= 0)
                {
                    return RecognisedDrives[n].MyDescription;
                }
                else
                {
                    return "No description";
                }
            }
            return "No description";
        }

        internal void RefreshDriveList()
        {
            foreach(Tache t in Kernel.Instance.JobProfiles.Jobs.Values)
            {
                var driveLabel = t.SourceVolume;
                var n = RememberedDriveIndex(driveLabel);
                if (n < 0)
                {
                    RememberedDrive klt = new RememberedDrive()
                    {
                        MyDescription = "Undefined",
                        VolumeLabel = driveLabel
                    };
                    RecognisedDrives.Add(klt);
                }
                driveLabel = t.DestinationVolume;
                n = RememberedDriveIndex(driveLabel);
                if (n < 0)
                {
                    RememberedDrive klt = new RememberedDrive()
                    {
                        MyDescription = "Undefined",
                        VolumeLabel = driveLabel
                    };
                    RecognisedDrives.Add(klt);
                }
            }
        }

        internal string CurrentDriveLetter(int i)
        {
            DetectedDrives dd = new DetectedDrives();
            char? let = dd.DriveLetter(RecognisedDrives[i].VolumeLabel);
            if (let.HasValue) { return let.Value.ToString(); } else { return string.Empty; }
        }

}