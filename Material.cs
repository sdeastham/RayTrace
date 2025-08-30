using System.Drawing;
using System.Numerics;
using System.Reflection;

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
        scattered.Overwrite(new Ray(rec.P, scatterDirection, rIn.Time));
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
        reflected = reflected.UnitVector + (Fuzz * generator.RandomUnitVector());
        scattered.Overwrite(new Ray(rec.P, reflected, rIn.Time));
        attenuation.Overwrite(Albedo);
        return Vector3d.Dot(scattered.Direction, rec.Normal) > 0.0;
    }
}

public class Dielectric(double refractiveIndex) : Material
{
    // Refractive index in vacuum or air, or the ratio of the material's
    // refractive index over the refractive index of the enclosing media
    private readonly double RefractiveIndex = refractiveIndex;
    public override bool Scatter(Ray rIn, HitRecord rec, Vector3d attenuation, Ray scattered, RTRandom generator)
    {
        attenuation.Overwrite(new Vector3d(1.0, 1.0, 1.0));
        double ri = rec.FrontFace ? (1.0 / RefractiveIndex) : RefractiveIndex;
        Vector3d unitDirection = rIn.Direction.UnitVector;
        double cosTheta = Math.Min(Vector3d.Dot(-unitDirection, rec.Normal), 1.0);
        double sinTheta = Math.Sqrt(1.0 - cosTheta * cosTheta);
        bool cannotRefract = ri * sinTheta > 1.0;
        Vector3d direction;

        if (cannotRefract || Reflectance(cosTheta, ri) > generator.RandomDouble())
        {
            direction = Vector3d.Reflect(unitDirection, rec.Normal);
        }
        else
        {
            direction = Vector3d.Refract(unitDirection, rec.Normal, ri);
        }
        scattered.Overwrite(new Ray(rec.P, direction, rIn.Time));
        return true;
    }

    private static double Reflectance(double cosine, double refractiveIndex)
    {
        // Use Schlick's approximation for reflectance
        var r0 = (1.0 - refractiveIndex) / (1.0 + refractiveIndex);
        r0 *= r0;
        return r0 + (1.0 - r0) * Math.Pow(1.0 - cosine, 5.0);
    }
}