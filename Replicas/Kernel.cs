using System;

namespace Replicas;

internal sealed class Kernel
{
    private Kernel()
        {
            // the constructor is private thus preventing instances other than the single private instance from being created
        }

        internal static Kernel Instance // this static property allows global access to the single private instance of this class

        { get; } = new Kernel();

        internal string CurrentTask;

        private ReplicaJobs _jobs;
        private DetectedDrives _locations;

        internal void ShutDown()
        {
            JobProfiles.SaveProfile();
            KnownDrives.SaveProfile();
        }

        internal ReplicaJobs JobProfiles
        {
            get
            {
                if (_jobs == null) { _jobs = new ReplicaJobs(); }
                return _jobs;
            }
        }

        internal RememberedDrives KnownDrives
        {
            get
            {
                if (KnownLocations == null) { KnownLocations = new RememberedDrives(); }

                return KnownLocations;
            }
        }

        internal DetectedDrives DrivesCurrentlyFound
        {
            get
            {
                if (_locations == null) { _locations = new DetectedDrives(); }

                return _locations;
            }
        }

        internal RememberedDrives KnownLocations { get; set; }

        internal static string HowLongAgo(DateTime lastDate)
        {
            string ans = "Never";
            if (lastDate < new DateTime(2010, 1, 1)) { return ans; }
            TimeSpan s = DateTime.Now - lastDate;
            int r = s.Days;
            if (r > 2) { return $"{r} days ago"; }
            r = Convert.ToInt32(s.TotalHours);
            if (r > 2) { return $"{r} hours ago"; }
            r = Convert.ToInt32(s.TotalMinutes);
            if (r == 1)
            { return "1 minute ago"; }
            else
            { return $"{r} minutes ago"; }
        }
        internal static string SizeReport(long byteCount)
        {
            string rv = $"{byteCount} bytes";
            if (byteCount >= 1000) { rv =$"{byteCount / 1000} KB"; }
            if (byteCount >= 1000000) { rv = $"{byteCount / 1000000} MB"; }
            if (byteCount >= 1000000000) { rv =$"{byteCount / 1000000000} GB"; }
            if (byteCount >= 1000000000000) { rv = $"{byteCount / 1000000000000} TB"; }
            return rv;
        }

        internal void ForceRefresh()
        {
            _jobs = null;
            _locations = null;
            _locations = new DetectedDrives();
            _jobs = new ReplicaJobs();
        }

        internal static string AttemptToSetFileAttributes(string path, System.IO.FileAttributes attribs)
        {
            string retVal = string.Empty;
            try
            {
                System.IO.File.SetAttributes(path, attribs);
            }
            catch (ArgumentException argex)
            {
                retVal = argex.Message;
            }
            catch (System.IO.PathTooLongException pathex)
            {
                retVal = pathex.Message;
            }
            catch (NotSupportedException nsupex)
            {
                retVal = nsupex.Message;
            }
            catch (System.IO.DirectoryNotFoundException dnfex)
            {
                retVal = dnfex.Message;
            }
            catch (System.IO.FileNotFoundException fnfex)
            {
                retVal = fnfex.Message;
            }
            catch (UnauthorizedAccessException unaaex)
            {
                retVal = unaaex.Message;
            }
            return retVal;
        }

        internal static string AttemptToDeleteFile(string path)
        {
            string msg = string.Empty;
            try
            {
                System.IO.File.Delete(path);
            }
            catch (ArgumentNullException ex)
            {
                msg = ex.Message;
            }
            catch (ArgumentException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.PathTooLongException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.IOException ex)
            {
                msg = ex.Message;
            }
            catch (NotSupportedException ex)
            {
                msg = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                msg = ex.Message;
            }
            if (!string.IsNullOrEmpty(msg)) { msg = "Failed to delete file: " + msg; }
            return msg;
        }

        internal static string AttemptToDeleteDirectory(string path)
        {
            string msg = string.Empty;
            try
            {
                System.IO.Directory.Delete(path);
            }
            catch (ArgumentNullException ex)
            {
                msg = ex.Message;
            }
            catch (ArgumentException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.PathTooLongException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.IOException ex)
            {
                msg = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                msg = ex.Message;
            }
            if (!string.IsNullOrEmpty(msg)) { msg = "Failed to delete directory: " + msg; }
            return msg;
        }

        internal static string AttemptToAddDirectory(string path)
        {
            string msg = string.Empty;
            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch (ArgumentNullException ex)//
            {
                msg = ex.Message;
            }
            catch (ArgumentException ex)//
            {
                msg = ex.Message;
            }
            catch (System.IO.DirectoryNotFoundException ex)//
            {
                msg = ex.Message;
            }
            catch (System.IO.PathTooLongException ex)//
            {
                msg = ex.Message;
            }
            catch (System.IO.IOException ex)//
            {
                msg = ex.Message;
            }
            catch (UnauthorizedAccessException ex)//
            {
                msg = ex.Message;
            }
            catch (NotSupportedException ex)
            {
                msg = ex.Message;
            }
            if (!string.IsNullOrEmpty(msg)) { msg = "Failed to create directory: " + msg; }
            return msg;
        }

        internal static string AttemptToCopyFile(string spath, string dpath, bool overwrite)
        {
            string msg = string.Empty;
            try
            {
                System.IO.File.Copy(spath, dpath, overwrite);
            }
            catch (ArgumentNullException ex)
            {
                msg = ex.Message;
            }
            catch (ArgumentException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.PathTooLongException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.IOException ex)
            {
                msg = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                msg = ex.Message;
            }
            catch (NotSupportedException ex)
            {
                msg = ex.Message;
            }
            if (!string.IsNullOrEmpty(msg)) { msg = "Failed to copy file: " + msg; }
            return msg;
        }

        internal static string[] AttemptToGetFiles(string folder, out string erreur)
        {
            string msg = string.Empty;
            string[] subfiles = Array.Empty<string>();
            try
            {
                subfiles = System.IO.Directory.GetFiles(folder);
            }
            catch (System.IO.PathTooLongException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                msg = ex.Message;
            }
            catch (System.IO.IOException ex)
            {
                msg = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                msg = ex.Message;
            }
            catch (ArgumentNullException ex)
            {
                msg = ex.Message;
            }
            catch (ArgumentException ex)
            {
                msg = ex.Message;
            }
            erreur = msg;
            return subfiles;
        }

        public static string SayTime(double q)
        {
            string u;
            long k;
            if (q < 60)
            {
                k = (long)Math.Floor(q); // k = sec
                u = $"{k} sec.";
            }
            else
            {
                if (q < 3600)
                {
                    k = (long)Math.Floor(q); // k = sec
                    u = $"{(long)Math.Floor(k / (double)60)} min. {k % 60} sec.";
                }
                else
                {
                    if (q < 86400)
                    {
                        k = (long)Math.Floor(q / 60); // k = min
                        u = $"{(long)Math.Floor(k / (double)60)} hr. {k % 60} min.";
                    }
                    else
                    {
                        if (q < 172800)
                        {
                            k = (long)Math.Floor(q / 3600); // k = hours
                            u = $"{(long)Math.Floor(k / (double)24)} day {k % 24} hr.";
                        }
                        else
                        {
                            k = (long)Math.Floor(q / 3600); // k = hours
                            u = $"{(long)Math.Floor(k / (double)24)} days {k % 24} hr.";
                        }
                    }
                }
            }
            return u;
        }

        public static void SoundSignal(int freq,int millisec)
        {
            Console.Beep(freq, millisec);
        }
}