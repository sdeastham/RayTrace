using System.Numerics;

namespace RayTrace;

static class RTVector
{
    public static Vector3 UnitVector(Vector3 v)
    {
        return v / v.Length();
    }
}