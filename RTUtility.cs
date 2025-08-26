using System.Numerics;

namespace RayTrace;

static class RTUtility
{
    public static Vector3 UnitVector(Vector3 v)
    {
        return v / v.Length();
    }
}

public class RTRandom
{
    private readonly Random Generator;

    public RTRandom()
    {
        Generator = new();
    }

    public double RandomDouble()
    {
        return Generator.NextDouble();
    }

    public double RandomDouble(double min, double max)
    {
        // Returns a random real in [min,max)
        return min + (max - min) * RandomDouble();
    }

    public float RandomFloat()
    {
        return (float)RandomDouble();
    }

    public float RandomFloat(float min, float max)
    {
        return min + (max - min) * RandomFloat();
    }
}