namespace Replicas;

internal class UpdaterResults
{
    public int FileDelSuccess { get; set; }
    public int FileDelFailure { get; set; }

    public int DirDelSuccess { get; set; }
    public int DirDelFailure { get; set; }

    public int DirAddSuccess { get; set; }
    public int DirAddFailure { get; set; }

    public int FileUpdSuccess { get; set; }
    public int FileUpdFailure { get; set; }
    public long FileUpdBytes { get; set; }
    public long FileUpdBytesTarget { get; set; }

    public int FileAddSuccess { get; set; }
    public int FileAddFailure { get; set; }
    public long FileAddBytes { get; set; }
    public long FileAddBytesTarget { get; set; }
    public string LargeFileName { get; set; }
    public long LargeFileSize { get; set; }
    public bool WasCancelled { get; set; }

    public bool AnyFailures => ((FileDelFailure + DirDelFailure + DirAddFailure + FileAddFailure + FileUpdFailure) > 0);

    public UpdaterResults()
    {
        LargeFileName = string.Empty;
        FileDelSuccess = 0;
        FileDelFailure = 0;
        DirDelSuccess = 0;
        DirDelFailure = 0;
        DirAddSuccess = 0;
        DirAddFailure = 0;
        FileUpdSuccess = 0;
        FileUpdFailure = 0;
        FileUpdBytes = 0;
        FileUpdBytesTarget = 0;
        FileAddSuccess = 0;
        FileAddFailure = 0;
        FileAddBytes = 0;
        FileUpdBytesTarget = 0;
        WasCancelled = false;
    }
}