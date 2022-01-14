using System;
using System.Collections.Generic;
using System.Globalization;

namespace Replicas;

internal class ReplicaJobs
{
    internal Dictionary<string, Tache> Jobs;

        // private readonly string _dataFileJobs = System.IO.Path.Combine(Jbh.AppManager.DataPath, "Jobs.jobs");
        private readonly string _dataFileTasks = System.IO.Path.Combine(Jbh.AppManager.DataPath, "Tasks.jht");

        private void LoadProfile()
        {
            Jobs.Clear();
            if (!System.IO.File.Exists(_dataFileTasks)) return;
            using (System.IO.StreamReader sr = new System.IO.StreamReader(_dataFileTasks))
            {
                Tache boulot = new Tache();
                while (!sr.EndOfStream)
                {
                    string? dat = sr.ReadLine();
                    if (dat is not { }) continue;
                    int p = dat.IndexOf(':');
                    if (p > 0) // ignores blank line
                    {
                        string itemTag = dat.Substring(0, p);
                        string itemContent = dat.Substring(p + 1);
                        switch (itemTag)
                        {
                            case "Job":
                            {
                                boulot = new Tache() {Key = itemContent};
                                Jobs.Add(boulot.Key, boulot);
                                break;
                            }
                            case "JobTitle":
                            {
                                boulot.JobTitle = itemContent;
                                break;
                            }
                            case "SourcePath":
                            {
                                boulot.SourcePath = itemContent;
                                break;
                            }
                            case "SourceVolume":
                            {
                                boulot.SourceVolume = itemContent;
                                break;
                            }
                            case "DestinationPath":
                            {
                                boulot.DestinationPath = itemContent;
                                break;
                            }
                            case "DestinationVolume":
                            {
                                boulot.DestinationVolume = itemContent;
                                break;
                            }
                            case "LastDate":
                            {
                                if (DateTime.TryParse(itemContent, CultureInfo.InvariantCulture, DateTimeStyles.None
                                        , out DateTime dt))
                                {
                                    boulot.LastDate = dt;
                                }

                                break;
                            }
                            case "IncludeHidden":
                            {
                                if (bool.TryParse(itemContent, out bool si))
                                {
                                    boulot.IncludeHidden = si;
                                }

                                break;
                            }
                            case "Dangerous":
                            {
                                if (bool.TryParse(itemContent, out bool si))
                                {
                                    boulot.Dangerous = si;
                                }

                                break;
                            }
                            case "IsJbhInfo":
                            {
                                if (bool.TryParse(itemContent, out bool si))
                                {
                                    boulot.IsJbhInfoBackup = si;
                                }

                                break;
                            }
                            case "IsJbhBusiness":
                            {
                                if (bool.TryParse(itemContent, out bool si))
                                {
                                    boulot.IsJbhBusinessBackup = si;
                                }

                                break;
                            }
                            case "ExpectedItemCount":
                            {
                                if (long.TryParse(itemContent, out long ct))
                                {
                                    boulot.ExpectedItemCount = ct;
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        internal void SaveProfile()
        {
            Jbh.AppManager.CreateBackupDataFile(_dataFileTasks);
            Jbh.AppManager.PurgeOldBackups("jht", 5, 5);
            
            if (string.IsNullOrWhiteSpace(_dataFileTasks)) return;
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(_dataFileTasks))
            {
                foreach (Tache j in Jobs.Values)
                {
                    sw.WriteLine(" ");
                    sw.WriteLine($"Job:{j.Key}");
                    sw.WriteLine($"JobTitle:{j.JobTitle}");
                    sw.WriteLine($"SourcePath:{j.SourcePath}");
                    sw.WriteLine($"SourceVolume:{j.SourceVolume}");
                    sw.WriteLine($"DestinationPath:{j.DestinationPath}");
                    sw.WriteLine($"DestinationVolume:{j.DestinationVolume}");
                    sw.WriteLine($"LastDate:{j.LastDate.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"IncludeHidden:{j.IncludeHidden.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"Dangerous:{j.Dangerous.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"IsJbhInfo:{j.IsJbhInfoBackup.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"IsJbhBusiness:{j.IsJbhBusinessBackup.ToString(CultureInfo.InvariantCulture)}");
                    sw.WriteLine($"ExpectedItemCount:{j.ExpectedItemCount.ToString(CultureInfo.InvariantCulture)}");
                }
            }
        }

        
        internal ReplicaJobs() // constructor
        {
            Jobs = new Dictionary<string, Tache>();
            LoadProfile();
        }
        
        internal string AddJob(string jTitle, string sPath, string sVolume, string dPath, string dVolume, bool dHidden,bool dDangerous, bool dJbhInfo,bool dJbhBusiness)
        {
            Tache nw = new Tache()
            {
                JobTitle = jTitle,
                SourcePath = sPath,
                SourceVolume = sVolume,
                DestinationPath = dPath,
                DestinationVolume = dVolume,
                IncludeHidden = dHidden,
                Dangerous=dDangerous,
                IsJbhInfoBackup=dJbhInfo,
                IsJbhBusinessBackup=dJbhBusiness,
                LastDate = new DateTime(year: 1954, month: 1, day: 1)
            };
            nw.Key = SpareKey();
            Jobs.Add(nw.Key, nw);
            return nw.Key;
        }

        internal void UpdateJob(string key, string jTitle, string sPath, string sVolume,
            string dPath, string dVolume, bool dHidden,bool dDangerous, bool dJbhInfo, bool dJbhBusiness)
        {
           Tache nw= Jobs[key];
            nw.JobTitle = jTitle;
            nw.SourcePath = sPath;
            nw.SourceVolume = sVolume;
            nw.DestinationPath = dPath;
            nw.DestinationVolume = dVolume;
            nw.IncludeHidden = dHidden;
            nw.Dangerous = dDangerous;
            nw.IsJbhInfoBackup = dJbhInfo;
            nw.IsJbhBusinessBackup = dJbhBusiness;
        }

        internal List<string> JbhInfoDates()
        {
            string sortformat = "yyyy MM dd HH mm";
            int sortformatlength = sortformat.Length;
            List<string> templist = new List<string>();
            foreach (Tache j in Jobs.Values)
            {
                if (j.IsJbhInfoBackup)
                {
                    DateTime d = j.LastDate;
                    string sorter = d.ToString(sortformat, CultureInfo.InvariantCulture);
                    var dateshow = d.ToString("d MMM yyyy a\\t HH:mm", CultureInfo.InvariantCulture);
                    var nm = j.JobTitle;
                    string all = sorter + dateshow + "^" + nm + "^" + Kernel.HowLongAgo(d);
                    templist.Add(all);
                }
                templist.Sort();
                templist.Reverse();
            }
            List<string> output = new List<string>();
            foreach (string a in templist) { output.Add(a.Substring(sortformatlength)); }
            return output;
        }

        internal List<string> JbhBusinessDates()
        {
            string sortformat = "yyyy MM dd HH mm";
            int sortformatlength = sortformat.Length;
            List<string> templist = new List<string>();
            foreach (Tache j in Jobs.Values)
            {
                if (j.IsJbhBusinessBackup)
                {
                    DateTime d = j.LastDate;
                    string sorter = d.ToString(sortformat, CultureInfo.InvariantCulture);
                    string dateshow = d.ToString("d MMM yyyy a\\t HH:mm", CultureInfo.InvariantCulture);
                    var nm = j.JobTitle;
                    string all =$"{sorter}{dateshow}^{nm}^{Kernel.HowLongAgo((d))}";
                    templist.Add(all);
                }
                templist.Sort();
                templist.Reverse();
            }
            List<string> output = new List<string>();
            foreach (string a in templist) { output.Add(a.Substring(sortformatlength)); }
            return output;
        }

        internal List<TimeSpan> JbhInfoAgos()
        {
            List<TimeSpan> templist = new List<TimeSpan>();
            foreach (Tache j in Jobs.Values)
            {
                if (j.IsJbhInfoBackup)
                {
                    templist.Add( DateTime.Now - j.LastDate);
                }
                templist.Sort();
            }
            return templist;
        }

        internal List<TimeSpan> JbhBusinessAgos()
        {
            List<TimeSpan> templist = new List<TimeSpan>();
            foreach (Tache j in Jobs.Values)
            {
                if (j.IsJbhBusinessBackup)
                {
                    templist.Add(DateTime.Now - j.LastDate);
                }
                templist.Sort();
            }
            return templist;
        }

        internal List<Tuple<int, int>> JobCounts()
        {
            int ina = 0, inn = 0, bua = 0, bun = 0, ota = 0, otn = 0;
            foreach (Tache j in Jobs.Values)
            {
                bool hid = j.PathsInaccessible();
                if (j.IsJbhBusinessBackup)
                {
                    if (hid) { bun++; } else { bua++; }
                    
                }
                else if (j.IsJbhInfoBackup)
                {
                    if (hid) { inn++; } else { ina++; }
                }
                else
                {
                    if (hid) { otn++; } else { ota++; }
                }
            }
            List<Tuple<int, int>> results = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(bua, bun),
                new Tuple<int, int>(ina, inn),
                new Tuple<int, int>(ota, otn)
            };
            return results;
        }

        internal void AllocateJbhInfoFreshnessRanking()
        {
            string sortformat = "yyyy MM dd HH mm";
            int sortformatlength = sortformat.Length;
            List<string> templist = new List<string>();
            foreach (Tache t in Jobs.Values)
            {
                if (t.IsJbhInfoBackup)
                {
                    t.Oldest = t.Newest = false;
                    DateTime d = t.LastDate;
                    string sorter = d.ToString(sortformat, CultureInfo.InvariantCulture);
                    string all = sorter + t.Key;
                    templist.Add(all);
                }
            }
            templist.Sort();
            string a = templist[0].Substring(sortformatlength);
            Jobs[a].Oldest = true;
            string z = templist[templist.Count - 1].Substring(sortformatlength);
            Jobs[z].Newest = true;
        }

        internal void AllocateJbhBusinessFreshnessRanking()
        {
            string sortformat = "yyyy MM dd HH mm";
            int sortformatlength = sortformat.Length;
            List<string> templist = new List<string>();
            foreach (Tache t in Jobs.Values)
            {
                if (t.IsJbhBusinessBackup)
                {
                    t.Oldest = t.Newest = false;
                    DateTime d = t.LastDate;
                    string sorter = d.ToString(sortformat, CultureInfo.InvariantCulture);
                    string all = sorter + t.Key;
                    templist.Add(all);
                }
            }
            templist.Sort();
            string a = templist[0].Substring(sortformatlength);
            Jobs[a].Oldest = true;
            string z = templist[templist.Count - 1].Substring(sortformatlength);
            Jobs[z].Newest = true;
        }

        private string SpareKey()
        {
            string k;
            do
            {
                Random rng = new Random();
                int i = rng.Next(1, int.MaxValue);
                k = $"{i:X}";
            } while (Jobs.ContainsKey(k));
            return k;
        }
}