namespace Replicas;

internal class ProgressInfo
{
    public ProgressInfo(int pcs,int pcn, char ph, UpdaterResults? rs)
    {
        Phase = ph;
        PercentSize = pcs;
        PercentNumber = pcn;
        Results = rs;
    }
    public char Phase { get; }
    public int PercentSize { get; }
    public int PercentNumber { get; }
    public UpdaterResults? Results {get; }
}