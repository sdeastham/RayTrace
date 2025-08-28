using System.Numerics;
using System.Reflection.Metadata;

namespace RayTrace;

public class Ray(Vector3d origin, Vector3d direction)
{
    public Vector3d Origin { get; private set; } = origin;
    public Vector3d Direction { get; private set; } = direction;

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