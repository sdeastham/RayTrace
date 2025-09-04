using System.Drawing;
using System.Numerics;
using System.Reflection;

namespace RayTrace;

public interface IMaterial
{
    bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator);
    double ScatteringPDF(Ray rIn, HitRecord rec, Ray scattered);
}

public class Material : IMaterial
{
    public virtual bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec,RTRandom generator)
    {
        return false;
    }
    public virtual Color Emitted(Ray rIn, HitRecord rec, double u, double v, Vector3d p)
    {
        return Color.Black;
    }
    public virtual double ScatteringPDF(Ray rIn, HitRecord rec, Ray scattered)
    {
        return 0.0;
    }
}

public class Lambertian(ITexture tex) : Material
{
    public Lambertian(Color albedo) : this(new SolidColor(albedo)) { }

    protected ITexture Tex = tex;

    public override bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator)
    {
        sRec.Attenuation = Tex.Value(rec.U, rec.V, rec.P);
        sRec.SourcePDF = new CosinePDF(rec.Normal);
        sRec.SkipPDF = false;
        return true;
    }

    public override double ScatteringPDF(Ray rIn, HitRecord rec, Ray scattered)
    {
        double cosine = Vector3d.Dot(rec.Normal, scattered.Direction.UnitVector);
        return cosine < 0.0 ? 0.0 : cosine / Math.PI;
    }
}

public class Metal(Color albedo, double fuzz) : Material
{
    private readonly Color Albedo = albedo;
    private readonly double Fuzz = fuzz < 1.0 ? fuzz : 1.0;

    public override bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator)
    {
        Vector3d reflected = Vector3d.Reflect(rIn.Direction, rec.Normal);
        reflected = reflected.UnitVector + (Fuzz * generator.RandomUnitVector());
        sRec.Attenuation = Albedo;
        sRec.SourcePDF = null;
        sRec.SkipPDF = true;
        sRec.SkipPDFRay = new Ray(rec.P, reflected, rIn.Time);
        return true;
    }
}

public class Dielectric(double refractiveIndex) : Material
{
    // Refractive index in vacuum or air, or the ratio of the material's
    // refractive index over the refractive index of the enclosing media
    private readonly double RefractiveIndex = refractiveIndex;
    public override bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator)
    {
        sRec.Attenuation = Color.White;
        sRec.SourcePDF = null;
        sRec.SkipPDF = true;
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
        sRec.SkipPDFRay = new Ray(rec.P, direction, rIn.Time);
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

public class DiffuseLight(ITexture tex) : Material
{
    public DiffuseLight(Color emit) : this(new SolidColor(emit)) { }
    public override Color Emitted(Ray rIn, HitRecord rec, double u, double v, Vector3d p)
    {
        // Light is only emitted on the front face
        if (!rec.FrontFace) return Color.Black;
        return Tex.Value(u, v, p);
    }
    protected ITexture Tex = tex;
}

public class Isotropic(ITexture tex) : Material
{
    public Isotropic(Color albedo) : this(new SolidColor(albedo)) { }
    protected ITexture Tex = tex;
    public override bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator)
    {
        sRec.Attenuation = Tex.Value(rec.U, rec.V, rec.P);
        sRec.SourcePDF = new SpherePDF();
        sRec.SkipPDF = false;
        return true;
    }
    public override double ScatteringPDF(Ray rIn, HitRecord rec, Ray scattered)
    {
        return 1.0 / (4.0 * Math.PI);
    }
}

public class ScatterRecord
{
    public Color Attenuation;
    public PDF? SourcePDF;
    public bool SkipPDF;
    public Ray? SkipPDFRay;
}