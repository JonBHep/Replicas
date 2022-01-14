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
    public char Phase { get; set; }
    public int PercentSize { get; set; }
    public int PercentNumber { get; set; }
    public UpdaterResults? Results {get; set;}
}