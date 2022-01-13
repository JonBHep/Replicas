using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Jbh
{
    internal static class AppManager
    {
        /// <summary>
        /// 
        /// Add the following commented-out section to the App.Xaml.cs file (it assumes the startup window will be called MainWindow)
        /// 
        /// Then add 
        /// Startup="Application_Startup" Exit="Application_Exit"
        /// to the App.xaml file in place of "StartupUri=...
        /// 
        /// </summary>

        // NOTE All edits to AppManager.cs should be copied to the reference version in Code\Common\Jbh.Classes. The Launcher/My Applications screen will show which apps have an out-of-date AppManager file
        // using the .Net Framework and .Net6.0 versions as the definitive model
        // This file edited 13-01-2022

        /*
         
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool okToLaunch = true;
            string title = Jbh.AppManager.AppName;
            if (string.IsNullOrWhiteSpace(Jbh.AppManager.DataPath))
            {
                MessageBox.Show("The folder for the " + title + " application data within the Jbh.Info\\AppData directory has not been found.\n\nThe application will now be shut down.", title, MessageBoxButton.OK, MessageBoxImage.Error);
                okToLaunch = false;
            }
            else
            {
                DateTime? launched = Jbh.AppManager.RunStart;
                if (launched.HasValue)
                {

                    MessageBoxResult answer = MessageBox.Show(title + " appears to be already running.\n\nLaunched at " + launched.Value.ToShortTimeString() + " on " + launched.Value.ToShortDateString() + "\n\nOnly continue if you are sure that " + title + " is not currently running.\n\nContinue?", title, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (answer != MessageBoxResult.OK)
                    {
                        okToLaunch = false;
                    }
                }
            }
            if (okToLaunch)
            {
                Jbh.AppManager.SetRunStart();
                MainWindow w = new MainWindow();
                w.Show();
            }
            else
            {
                App.Current.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Jbh.AppManager.DeleteRuntimeFile();
        }
        
        */

        private static string? _dataPath;
        // .NET 6 version: private static string? _dataPath;

        internal static string DataPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_dataPath)) { _dataPath = UsualDataPath; }
                return _dataPath;
            }
        }

        internal static string AppName
        {
            get
            {
                string apNam = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                // if (apNam.EndsWith(".vshost", StringComparison.InvariantCultureIgnoreCase)) { apNam = apNam.Substring(0, apNam.Length - 7); } // in case running in IDE
		if (apNam.EndsWith(".vshost", StringComparison.InvariantCultureIgnoreCase)) { apNam = apNam[0..^7]; } // in case running in IDE
                return apNam;
            }
        }

        internal static DateTime? RunStart
        {
            get
            {
                DateTime? retVal = null;
                if (File.Exists(RunTimeFile))
                {
                    string s = File.ReadAllText(RunTimeFile);
                    DateTime runTimeStart = DateTime.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                    retVal = runTimeStart;
                }
                return retVal;
            }
        }

        private static string RunTimeFile
        {
            get
            {
                return Path.Combine(DataPath, "_runtime.txt");
            }
        }

        internal static void SetRunStart()
        {
            DateTime start = DateTime.Now;
            File.WriteAllText(RunTimeFile, start.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        internal static void DeleteRuntimeFile()
        {
            File.Delete(RunTimeFile);
        }

        private static string UsualDataPath
        {
            // Revised Àugust 2021
            // Defines the data directory based on whether the app is running on the C drive of the home computer or on another drive letter indicating that it is running on the travelling USB drive

            // Assumes that on the home (C) drive the location of the Jbh.Info folder which contains the AppData folder is path is [Root]\Jbh.Original\Jbh.Info
            // And that in the case of my travelling USB drive the path is [Root]\Jbh.Portable\Jbh.Info
            get
            {
                string myPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DriveInfo di = new(myPath);
                string runLocationMessage;
                string appDataPath;
                if (di.RootDirectory.Name.StartsWith("C", StringComparison.InvariantCultureIgnoreCase))
                {
                    // We are installed on the current machine, or run from an EXE on the current machine
                    runLocationMessage = "Running on C drive";
                    appDataPath = Path.Combine(di.RootDirectory.FullName, "Jbh.Original", "Jbh.Info", "AppData", AppName);
                }
                else
                {
                    // We are running on my travelling USB drive
                    runLocationMessage = $"Running on travelling drive: {di.VolumeLabel}";
                    appDataPath = Path.Combine(di.RootDirectory.FullName, "Jbh.Portable", "Jbh.Info", "AppData", AppName);
                }
                if (!string.IsNullOrEmpty(appDataPath) && Directory.Exists(appDataPath)) { return appDataPath; }
                System.Windows.MessageBox.Show($"{runLocationMessage}\n\nFailed to locate the AppData path: {appDataPath}", AppName, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); return string.Empty;
            }
        }

        internal static void CreateBackupDataFile(string dataFileSpec)
        {
            string extn = Path.GetExtension(dataFileSpec);
            FileInfo fi = new(dataFileSpec);
            string timeStamp = $"{fi.LastWriteTimeUtc:yyyy-MM-dd-HH-mm-ss}";
            string backupPath = Path.Combine(DataPath, $"Backup-{timeStamp}{extn}");
            if (File.Exists(backupPath)) { File.Delete(backupPath); }
            if (File.Exists(dataFileSpec)) { File.Copy(dataFileSpec, backupPath); }
        }

        internal static void PurgeOldBackups(string fileExtension, int minimumDaysToKeep, int minimumFilesToKeep)
        {
            if (!fileExtension.StartsWith(".", StringComparison.InvariantCultureIgnoreCase)) { fileExtension = "." + fileExtension; }
            List<string> previousFiles = Directory.GetFiles(DataPath, "Backup-????-??-??-??-??-??" + fileExtension, SearchOption.TopDirectoryOnly).ToList();
            if (previousFiles.Count <= minimumFilesToKeep) { return; }

            int overAged;
            do
            {
                overAged = 0;
                string oldest = string.Empty;
                DateTime oldestDate = DateTime.MaxValue;
                foreach (string s in previousFiles)
                {
                    FileInfo fi = new(s);
                    TimeSpan fileAge = DateTime.UtcNow - fi.LastWriteTimeUtc;
                    if (fileAge.TotalDays > minimumDaysToKeep)
                    {
                        overAged++;
                        if (fi.LastWriteTimeUtc < oldestDate) { oldestDate = fi.LastWriteTimeUtc; oldest = s; }
                    }
                }
                if (!string.IsNullOrWhiteSpace(oldest)) { File.Delete(oldest); previousFiles.Remove(oldest); overAged--; }
            } while ((previousFiles.Count > minimumFilesToKeep) && (overAged > 0));
        }
        
	public static string VersionString()
        {
            Version? versionInfo = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return versionInfo is null ? string.Empty : $"{versionInfo.Major}.{versionInfo.Minor}";
        }

    }
}

