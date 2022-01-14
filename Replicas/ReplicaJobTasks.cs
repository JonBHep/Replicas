using System;
using System.Collections.Generic;
using System.Linq;

namespace Replicas;

internal class ReplicaJobTasks
{
    internal ReplicaJobTasks() // constructor
        {
            _masterActionList.Clear();
        }

        private readonly Dictionary<int, ReplicaAction> _masterActionList = new Dictionary<int, ReplicaAction>();

        internal void AddTask(string code, string path, string rationale, long mass)
        {
            int k = FreshKey;
            _masterActionList.Add(k, new ReplicaAction(k, code, path,mass, rationale));
        }

        public long TotalBulk()
        {
            long tot = 0;
            foreach(ReplicaAction ra in _masterActionList.Values)
            {
                tot += ra.Bulk;
            }
            return tot;
        }

        public long FileAddBulk()
        {
            long tot = 0;
            foreach (ReplicaAction ra in _masterActionList.Values)
            {
                if (ra.ActionCode.Equals("FA", StringComparison.OrdinalIgnoreCase))
                {
                    tot += ra.Bulk;
                }
            }
            return tot;
        }

        public long FileUpdBulk()
        {
            long tot = 0;
            foreach (ReplicaAction ra in _masterActionList.Values)
            {
                if (ra.ActionCode.Equals("FU", StringComparison.OrdinalIgnoreCase))
                {
                    tot += ra.Bulk;
                }
            }
            return tot;
        }

        internal int TaskCount(string code)
        {
            int c = 0;
            if (code.Equals("ER", StringComparison.OrdinalIgnoreCase))
            {
                foreach (ReplicaAction a in _masterActionList.Values)
                {
                    if (a.HadError) { c++; }
                }
            }
            else
            {
                foreach (ReplicaAction a in _masterActionList.Values)
                {
                    if (a.ActionCode.Equals(code, StringComparison.OrdinalIgnoreCase)) { c++; }
                }
            }
            return c;
        }

        internal List<ReplicaAction> AllTasks()
        {
            return _masterActionList.Values.ToList();
        }

        internal List<ReplicaAction> TaskList(string code)
        {
            List<ReplicaAction> list = new List<ReplicaAction>();
            if (code.Equals("ER",  StringComparison.OrdinalIgnoreCase ))
            {
                foreach (ReplicaAction act in _masterActionList.Values)
                {
                    if (act.HadError) { list.Add(act); }
                }
            }
            else
            {
                foreach (ReplicaAction act in _masterActionList.Values)
                {
                    if (act.ActionCode.Equals(code, StringComparison.OrdinalIgnoreCase)) { list.Add(act); }
                }
            }

            list.Sort();

            //reverse the sort for directory deletion to ensure hierarchical deletion
            if (code.Equals("DD", StringComparison.OrdinalIgnoreCase)) { list.Reverse(); } // 'sort' followed by 'reverse' is required, as 'reverse' reverses the order without sorting
            return list;
        }

        internal bool PathAndFilenameLengthsOk()
        {
            bool anyProblem = false;

            foreach (ReplicaAction thing in _masterActionList.Values)
            {
                var f = thing.DestinationPath;
                if (f.Length >= 260)
                {
                    thing.ErrorText = "Filename with path >= 260 characters: " + f;
                    anyProblem = true;
                }
                else
                {
                    f = System.IO.Path.GetDirectoryName(f);
                    if (f is null || f.Length < 248) continue;
                    thing.ErrorText = "Directory path >= 248 characters: " + f;
                    anyProblem = true;
                }
            }
            return !anyProblem;
        }

        internal int GrandTotal => _masterActionList.Count;

        private int FreshKey
        {
            get
            {
                Random r = new Random();
                int i = r.Next();
                while (_masterActionList.ContainsKey(i))
                {
                    i = r.Next();
                }
                return i;
            }
        }
}