using System.Numerics;
using System.Reflection.Metadata;

namespace RayTrace;

class Ray(Vector3 origin, Vector3 direction)
{
    public Vector3 Origin { get; private set; } = origin;
    public Vector3 Direction { get; private set; } = direction;

    public Vector3 At(float t)
    {
        return Origin + t * Direction;
    }
}