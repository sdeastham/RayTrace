using System.Drawing;
using System.Reflection.Metadata;

namespace RayTrace;

public class Interval
{
    public float Min { get; private set; }
    public float Max { get; private set; }

    public Interval()
    {
        // Default to an empty interval
        Min = float.PositiveInfinity;
        Max = float.NegativeInfinity;
    }

    public Interval(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public float Size => Max - Min;

    public bool Contains(float x) => Min <= x && x <= Max;

    public bool Surrounds(float x) => Min < x && x < Max;

    public static readonly Interval empty = new();
    public static readonly Interval universe = new(float.NegativeInfinity, float.PositiveInfinity);

}
