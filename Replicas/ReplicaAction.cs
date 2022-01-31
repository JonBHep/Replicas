using System;

namespace Replicas;

internal class ReplicaAction : IComparable<ReplicaAction>
{
    public int KeyInt { get; }
    public string ActionCode { get; set; }
    public string DestinationPath { get; set; }
    public string Rationale { get; set; }
    public bool HadError { get { return !string.IsNullOrWhiteSpace(ErrorText); } }
    public string ErrorText { get; set; }
    public long Bulk { get; set; }
    public ReplicaAction(int key, string aCode, string aDestination, long size, string aRationale) // constructor
    {
        KeyInt = key;
        ActionCode = aCode;
        DestinationPath = aDestination;
        Rationale = aRationale;
        ErrorText = string.Empty;
        Bulk = size;
    }
    public string ItemName
    {
        get
        {
            return System.IO.Path.GetFileName(DestinationPath);
        }
    }
    public string DestinationFolder
    {
        get
        {
            return System.IO.Path.GetDirectoryName(DestinationPath);
        }
    }
    public int CompareTo(ReplicaAction? obj)
    {
        if (obj is { })
        {
            return string.Compare(DestinationPath, obj.DestinationPath, StringComparison.OrdinalIgnoreCase);    
        }

        return 0;
    }
    public string SourcePath(string destinationStem, string sourceStem)
    {
        string f = DestinationPath.Substring(destinationStem.Length);
        return sourceStem + f;
    }

}