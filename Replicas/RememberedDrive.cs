using System;
using System.Globalization;

namespace Replicas;

internal class RememberedDrive : IComparable<RememberedDrive>
{
    internal string VolumeLabel { get; set; }
    internal string MyDescription { get; set; }
    internal long Capacity { get; set; }
    internal int PercentUsed { get; set; }
    internal long LastConnected { get; set; }
    internal int JobReferences { get; set; } // runtime only

    internal string Specification
    {
        get
        {
            return $"{VolumeLabel}@{MyDescription}@{Capacity}@{PercentUsed}@{LastConnected}";
        }
        set
        {
            string[] part = value.Split("@".ToCharArray());
            VolumeLabel = part[0];
            MyDescription = part[1];
            Capacity = long.Parse(part[2], CultureInfo.InvariantCulture);
            PercentUsed = int.Parse(part[3], CultureInfo.InvariantCulture);
            LastConnected = long.Parse(part[4], CultureInfo.InvariantCulture);
        }
    }
    int IComparable<RememberedDrive>.CompareTo(RememberedDrive? other)
    {
        if (other is { })
        {
            return string.Compare(MyDescription, other.MyDescription, StringComparison.OrdinalIgnoreCase);    
        }

        return 0;
    }

}