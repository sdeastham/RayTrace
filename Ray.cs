using System.Numerics;
using System.Reflection.Metadata;

namespace RayTrace;

public class Ray
{
    public Vector3d Origin { get; private set; }
    public Vector3d Direction { get; private set; }
    public double Time { get; private set; }

    public Ray(Vector3d origin, Vector3d direction) : this(origin, direction, 0.0){}
    public Ray(Vector3d origin, Vector3d direction, double time)
    {
        Origin = origin;
        Direction = direction;
        Time = time;
    }

    public Vector3d At(double t)
    {
        return Origin + t * Direction;
    }

    public void Overwrite(Ray replacement)
    {
        Origin = replacement.Origin;
        Direction = replacement.Direction;
    }
}