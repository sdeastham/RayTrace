using System.Drawing;
using System.Reflection.Metadata;

namespace RayTrace;

public class Interval
{
    public double Min { get; set; }
    public double Max { get; set; }

    public override string ToString()
    {
        return $"[{Min}, {Max}]";
    }

    // Constructors
    public Interval()
    {
        // Default to an empty interval
        Min = double.PositiveInfinity;
        Max = double.NegativeInfinity;
    }

    public Interval(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public Interval(Interval a, Interval b)
    {
        // Create the interval tightly enclosing the two input intervals
        Min = a.Min <= b.Min ? a.Min : b.Min;
        Max = a.Max >= b.Max ? a.Max : b.Max;
    }

    public double Clamp(double x)
    {
        if (x < Min) return Min;
        if (x > Max) return Max;
        return x;
    }

    public Interval Expand(double delta)
    {
        double padding = delta / 2.0;
        return new Interval(Min - padding, Max + padding);
    }

    public double Size => Max - Min;

    public bool Contains(double x) => Min <= x && x <= Max;

    public bool Surrounds(double x) => Min < x && x < Max;

    public static readonly Interval empty = new();
    public static readonly Interval universe = new(double.NegativeInfinity, double.PositiveInfinity);
    
    public static Interval operator +(Interval interval, double offset) => new(interval.Min + offset, interval.Max + offset);
    public static Interval operator +(double offset, Interval interval) => interval + offset;
}
