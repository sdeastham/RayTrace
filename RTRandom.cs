namespace RayTrace;

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