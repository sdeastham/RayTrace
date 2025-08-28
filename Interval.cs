using System.Drawing;
using System.Reflection.Metadata;

namespace RayTrace;

public class Interval
{
    public double Min { get; private set; }
    public double Max { get; private set; }

    public Interval()
    {
        // Default to an empty interval
        Min = float.PositiveInfinity;
        Max = float.NegativeInfinity;
    }

    public Interval(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public double Clamp(double x)
    {
        if (x < Min) return Min;
        if (x > Max) return Max;
        return x;
    }

    public double Size => Max - Min;

    public bool Contains(double x) => Min <= x && x <= Max;

    public bool Surrounds(double x) => Min < x && x < Max;

    public static readonly Interval empty = new();
    public static readonly Interval universe = new(double.NegativeInfinity, double.PositiveInfinity);

}
