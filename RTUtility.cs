using System.Numerics;

namespace RayTrace;

static class RTUtility
{
    public static Vector3d UnitVector(Vector3d v)
    {
        return v / v.Length;
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

    public Vector3d RandomVector()
    {
        return new(RandomDouble(), RandomDouble(), RandomDouble());
    }

    public Vector3d RandomVector(double min, double max)
    {
        return new(RandomDouble(min, max), RandomDouble(min, max), RandomDouble(min, max));
    }

    public Vector3d RandomUnitVector()
    {
        while (true)
        {
            var p = RandomVector(-1.0, 1.0);
            var lensq = p.LengthSquared;
            if (1e-160 < lensq && lensq <= 1.0)
            {
                return p / Math.Sqrt(lensq);
            }
        }
    }

    public Vector3d RandomVectorOnHemisphere(Vector3d normal)
    {
        Vector3d onUnitSphere = RandomUnitVector();
        // Check: is the unit vector in the same hemisphere as the normal?
        // If so, it is pointing out of the sphere. Otherwise it must be inverted 
        if (Vector3d.Dot(onUnitSphere, normal) > 0.0)
        {
            return onUnitSphere;
        }
        else
        {
            return -onUnitSphere;
        }
    }
}

public class Vector3d
{
    public double X { get; private set; }
    public double Y { get; private set; }
    public double Z { get; private set; }

    public Vector3d(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double LengthSquared => X * X + Y * Y + Z * Z;
    public double Length => Math.Sqrt(LengthSquared);

    public void Overwrite(Vector3d v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }

    public bool NearZero
    {
        get
        {
            double tolerance = 1.0e-8;
            return (Math.Abs(X) < tolerance && Math.Abs(Y) < tolerance && Math.Abs(Z) < tolerance);
        }
    }

    public static Vector3d Reflect(Vector3d v, Vector3d normal)
    {
        return v - 2.0 * Dot(v, normal) * normal;
    }

    public Vector3d UnitVector => this / Length;
    public static double Dot(Vector3d v1, Vector3d v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;

    // Operators
    public static Vector3d operator +(Vector3d v, double d) => new(v.X + d, v.Y + d, v.Z + d);
    public static Vector3d operator +(double d, Vector3d v) => v + d;
    public static Vector3d operator +(Vector3d v) => v;
    public static Vector3d operator +(Vector3d v1, Vector3d v2) => new(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);

    public static Vector3d operator -(Vector3d v, double d) => new(v.X - d, v.Y - d, v.Z - d);
    public static Vector3d operator -(double d, Vector3d v) => (-v) + d;
    public static Vector3d operator -(Vector3d v) => new(-v.X, -v.Y, -v.Z);
    public static Vector3d operator -(Vector3d v1, Vector3d v2) => new(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);

    public static Vector3d operator *(Vector3d v, double d) => new(v.X * d, v.Y * d, v.Z * d);
    public static Vector3d operator *(double d, Vector3d v) => v * d;
    public static Vector3d operator *(Vector3d v, int i) => v * (double)i;
    public static Vector3d operator *(int i, Vector3d v) => (double)i * v;
    public static Vector3d operator *(Vector3d v1, Vector3d v2) => new(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);

    public static Vector3d operator /(Vector3d v, double d) => new(v.X / d, v.Y / d, v.Z / d);
    public static Vector3d operator /(double d, Vector3d v) => new(d / v.X, d / v.Y, d / v.Z);
    public static Vector3d operator /(Vector3d v, int i) => v / (double)i;
    public static Vector3d operator /(int i, Vector3d v) => (double)i / v;
    public static Vector3d operator /(Vector3d v1, Vector3d v2) => new(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
}