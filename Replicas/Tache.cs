using System;

namespace Replicas;

internal class Tache : IComparable<Tache>
{
    public string? JobTitle { get; set; }
    public string? SourcePath { get; set; }
    public string? DestinationPath { get; set; }
    public string? SourceVolume { get; set; }
    public string? DestinationVolume { get; set; }
    public bool IncludeHidden { get; set; }

    public bool
        Dangerous
    {
        get;
        set;
    } // e.g. 'After travel' job which is run after travelling to restore Jbh.Info on desktop - if run at another time this would overwrite recent data

    public bool IsJbhInfoBackup { get; set; }
    public bool IsJbhBusinessBackup { get; set; }

    public bool IsJbhSpecialBackup => IsJbhBusinessBackup || IsJbhInfoBackup;

    public DateTime LastDate { get; set; }

    public long ExpectedItemCount { get; set; }
    //private int DynamicInterval { get; set; } // For Jbh.Info or Jbh.Business backups this is the dynamically calculated urgency based on a freshness ranking, aiming to rotate DB backups at multiples of the specified interval (below)
    //public void SetDynamicInterval(int i) { DynamicInterval = i; }

    public const int DaysIntervalBetweenRotatingJbhBackups = 3;

    //public int DynamicPriority { get; set; } // For Jbh.Info or Jbh.Business backups this indicates the order in which the rotating backups should be made
    public bool Oldest { get; set; }
    public bool Newest { get; set; }
    public string? Key { get; set; }

    public int CompareTo(Tache? other)
    {
        return other is { } ? string.Compare(JobTitle, other.JobTitle, StringComparison.CurrentCultureIgnoreCase) : 0;
    }

    internal string PeriodSinceLastRun()
    {
        return Kernel.HowLongAgo(LastDate);
    }

    internal int DuePercent()
    {
        if (!IsJbhSpecialBackup)
        {
            return -1;
        }

        if (!Newest)
        {
            return -2;
        } // only show a freshness colour for the newest backup

        return DuePercentage(LastDate);
    }

    private static int DuePercentage(DateTime lastDate)
    {
        if (lastDate < new DateTime(year: 2010, month: 1, day: 1)) return 999;
        TimeSpan s = DateTime.Now - lastDate;
        TimeSpan i = TimeSpan.FromDays(3);
        return Convert.ToInt32(100 * (s.TotalMinutes / i.TotalMinutes));
    }

    internal System.Windows.Media.SolidColorBrush DueColorBrush()
    {
        int percent = DuePercent();
        if (percent == -1)
        {
            return System.Windows.Media.Brushes.CornflowerBlue;
        } // no interval applies to this job

        if (percent == -2)
        {
            return System.Windows.Media.Brushes.Ivory;
        } // not the most recent backup of its kind

        System.Windows.Media.Color colourOne = System.Windows.Media.Colors.Green;
        System.Windows.Media.Color colourTwo = System.Windows.Media.Colors.Red;
        const int startIndex = 75;
        int endIndex = 125;

        System.Windows.Media.SolidColorBrush scb;

        if (percent < 76)
        {
            scb = new System.Windows.Media.SolidColorBrush(colourOne);
        }
        else if (percent > 125)
        {
            scb = new System.Windows.Media.SolidColorBrush(colourTwo);
        }
        else
        {
            byte rO = colourOne.R;
            byte gO = colourOne.G;
            byte bO = colourOne.B;
            byte rT = colourTwo.R;
            byte gT = colourTwo.G;
            byte bT = colourTwo.B;
            float steps = endIndex - startIndex;
            percent -= startIndex;
            byte rM = (byte) (rO + (percent * ((byte) ((rT - rO) / steps))));
            byte gM = (byte) (gO + (percent * ((byte) ((gT - gO) / steps))));
            byte bM = (byte) (bO + (percent * ((byte) ((bT - bO) / steps))));
            scb = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(a: 255, r: rM, g: gM, b: bM));
        }

        return scb;
    }

    internal string SourceFoundDrivePath()
    {
        // substitute current drive letter according to volume label
        var p = SourcePath;
        var v = SourceVolume;
        if ((p is not {})||(v is not {}))
        {
            return string.Empty;
        }
        DetectedDrives loci = new DetectedDrives();
        var l = loci.DriveLetter(v, p);
        if ((l is not {})||(SourcePath is not {}))
        {
            return string.Empty;
        }
        else
        {
            var pathOnFoundDrive = l + SourcePath[1..];
            return System.IO.Directory.Exists(pathOnFoundDrive) ? pathOnFoundDrive : string.Empty;
        }
    }

    internal string DestinationFoundDrivePath()
    {
        // substitute current drive letter according to volume label
        var p = DestinationPath;
        var v = DestinationVolume;
        if ((p is not { }) || (v is not { }))
        {
            return string.Empty;
        }
        DetectedDrives loci = new DetectedDrives();
        var l = loci.DriveLetter(v, p);
        if (l == null)
        {
            return string.Empty;
        }
        else
        {
            if (DestinationPath is not { })
            {
                return string.Empty;
            }
            var pathOnFoundDrive = l + DestinationPath.Substring(1);
            return System.IO.Directory.Exists(pathOnFoundDrive) ? pathOnFoundDrive : string.Empty;
        }
    }

    internal long DestinationFree()
    {
        string fddrv = DestinationFoundDrivePath();
        if (string.IsNullOrWhiteSpace(fddrv))
        {
            return -1;
        }

        System.IO.DriveInfo di = new System.IO.DriveInfo(fddrv);
        return di.AvailableFreeSpace;
    }

    internal long DestinationSize()
    {
        string fddrv = DestinationFoundDrivePath();
        if (string.IsNullOrWhiteSpace(fddrv))
        {
            return -1;
        }

        System.IO.DriveInfo di = new System.IO.DriveInfo(fddrv);
        return di.TotalSize;
    }

    internal long SourceFree()
    {
        string fddrv = SourceFoundDrivePath();
        if (string.IsNullOrWhiteSpace(fddrv))
        {
            return -1;
        }

        System.IO.DriveInfo di = new System.IO.DriveInfo(fddrv);
        return di.AvailableFreeSpace;
    }

    internal long SourceSize()
    {
        string fddrv = SourceFoundDrivePath();
        if (string.IsNullOrWhiteSpace(fddrv))
        {
            return -1;
        }

        System.IO.DriveInfo di = new System.IO.DriveInfo(fddrv);
        return di.TotalSize;
    }

    internal bool PathsInaccessible()
    {
        return string.IsNullOrWhiteSpace(SourceFoundDrivePath()) ||
               string.IsNullOrWhiteSpace(DestinationFoundDrivePath());
    }
}
    
