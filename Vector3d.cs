namespace RayTrace;

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

    public static Vector3d Refract(Vector3d uv, Vector3d normal, double indexRatio)
    {
        // uv -- Incoming vector (must be unit vector)
        // normal -- Surface normal
        // indexRatio -- refractive index of the material divided by that of the enclosing mediua
        var cosTheta = Math.Min(Dot(-uv, normal), 1.0);
        Vector3d rOutPerp = indexRatio * (uv + cosTheta * normal);
        Vector3d rOutParallel = -Math.Sqrt(Math.Abs(1.0 - rOutPerp.LengthSquared)) * normal;
        return rOutPerp + rOutParallel;
    }

    public Vector3d UnitVector => this / Length;
    public static double Dot(Vector3d v1, Vector3d v2) => v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;

    public static Vector3d Cross(Vector3d v1, Vector3d v2)
    {
        double e1 = v1.Y * v2.Z - v1.Z * v2.Y;
        double e2 = v1.Z * v2.X - v1.X * v2.Z;
        double e3 = v1.X * v2.Y - v1.Y * v2.X;
        return new(e1, e2, e3);
    }

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