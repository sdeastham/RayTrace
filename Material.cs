using System.Numerics;

namespace RayTrace;

public interface IMaterial
{
    bool Scatter(Ray rIn, HitRecord rec, Vector3d attenuation, Ray scattered, RTRandom generator);
}

public abstract class Material : IMaterial
{
    public abstract bool Scatter(Ray rIn, HitRecord rec, Vector3d attenuation, Ray scattered, RTRandom generator);
}

public class Lambertian(Vector3d albedo) : Material
{
    private Vector3d Albedo = albedo;

    public override bool Scatter(Ray rIn, HitRecord rec, Vector3d attenuation, Ray scattered, RTRandom generator)
    {
        var scatterDirection = rec.Normal + generator.RandomUnitVector();
        // Catch degenerate scatter direction
        if (scatterDirection.NearZero) scatterDirection = rec.Normal;
        scattered.Overwrite(new Ray(rec.P, scatterDirection));
        attenuation.Overwrite(Albedo);
        return true;
    }
}

public class Metal(Vector3d albedo, double fuzz) : Material
{
    private readonly Vector3d Albedo = albedo;
    private readonly double Fuzz = fuzz < 1.0 ? fuzz : 1.0;

    public override bool Scatter(Ray rIn, HitRecord rec, Vector3d attenuation, Ray scattered, RTRandom generator)
    {
        Vector3d reflected = Vector3d.Reflect(rIn.Direction, rec.Normal);
        reflected = reflected.UnitVector + (fuzz * generator.RandomUnitVector());
        scattered.Overwrite(new Ray(rec.P, reflected));
        attenuation.Overwrite(Albedo);
        return Vector3d.Dot(scattered.Direction,rec.Normal) > 0.0;
    }
}