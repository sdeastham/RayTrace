using System.Numerics;
using System.Reflection.Metadata;

namespace RayTrace;

public class Ray(Vector3d origin, Vector3d direction, double time = 0.0, double wavelength = 550.0e-9)
{
    public Vector3d Origin { get; private set; } = origin;
    public Vector3d Direction { get; private set; } = direction;
    public double Time { get; private set; } = time;
    public double Wavelength { get; private set; } = wavelength;

    public Ray(Vector3d origin, Vector3d direction) : this(origin, direction, 0.0, 550.0e-9) { }

    public Vector3d At(double t)
    {
        return Origin + t * Direction;
    }

    public void Overwrite(Ray replacement)
    {
        Origin = replacement.Origin;
        Direction = replacement.Direction;
        Time = replacement.Time;
        Wavelength = replacement.Wavelength;
    }
}