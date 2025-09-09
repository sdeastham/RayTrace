using System.Drawing;
using System.Numerics;
using System.Reflection;

namespace RayTrace;

public interface IMaterial
{
    bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator);
    double ScatteringPDF(Ray rIn, HitRecord rec, Ray scattered);
    double Emitted(Ray rIn, HitRecord rec, double u, double v, Vector3d p);
}

public class Material : IMaterial
{
    public virtual bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator) => false;
    public virtual double Emitted(Ray rIn, HitRecord rec, double u, double v, Vector3d p) => 0.0;
    public virtual double ScatteringPDF(Ray rIn, HitRecord rec, Ray scattered) => 0.0;
}

public class Lambertian(ITexture tex) : Material
{
    public Lambertian(ColorRGB albedo) : this(new SolidColor(albedo)) { }

    protected ITexture Tex = tex;

    public override bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator)
    {
        sRec.Attenuation = Tex.Value(rec.U, rec.V, rec.P, rIn.Wavelength);
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

public class Metal(double albedo, double fuzz) : Material
{
    private readonly double Albedo = albedo; // Assume same reflectivity at all wavelengths
    private readonly double Fuzz = fuzz < 1.0 ? fuzz : 1.0;

    public override bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator)
    {
        Vector3d reflected = Vector3d.Reflect(rIn.Direction, rec.Normal, rIn.Wavelength);
        reflected = reflected.UnitVector + (Fuzz * generator.RandomUnitVector());
        sRec.Attenuation = Albedo;
        sRec.SourcePDF = null;
        sRec.SkipPDF = true;
        sRec.SkipPDFRay = new Ray(rec.P, reflected, rIn.Time, rIn.Wavelength);
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
        sRec.Attenuation = 1.0; // No attenuation
        sRec.SourcePDF = null;
        sRec.SkipPDF = true;
        double wavelengthRefractiveIndex = CalculateRefractiveIndex(rIn.Wavelength);
        double ri = rec.FrontFace ? (1.0 / wavelengthRefractiveIndex) : wavelengthRefractiveIndex;
        Vector3d unitDirection = rIn.Direction.UnitVector;
        double cosTheta = Math.Min(Vector3d.Dot(-unitDirection, rec.Normal), 1.0);
        double sinTheta = Math.Sqrt(1.0 - cosTheta * cosTheta);
        bool cannotRefract = ri * sinTheta > 1.0;
        Vector3d direction;

        if (cannotRefract || Reflectance(cosTheta, ri) > generator.RandomDouble())
        {
            direction = Vector3d.Reflect(unitDirection, rec.Normal, rIn.Wavelength);
        }
        else
        {
            direction = Vector3d.Refract(unitDirection, rec.Normal, ri, rIn.Wavelength);
        }
        sRec.SkipPDFRay = new Ray(rec.P, direction, rIn.Time, rIn.Wavelength);
        return true;
    }

    private static double Reflectance(double cosine, double refractiveIndex)
    {
        // Use Schlick's approximation for reflectance
        var r0 = (1.0 - refractiveIndex) / (1.0 + refractiveIndex);
        r0 *= r0;
        return r0 + (1.0 - r0) * Math.Pow(1.0 - cosine, 5.0);
    }

    private double CalculateRefractiveIndex(double wavelength)
    {
        // Simple Cauchy equation for refractive index as a function of wavelength
        // wavelength is in meters; convert to micrometers for the equation
        double lambdaMicrometers = wavelength * 1e6;
        // Typical values for glass BK7: A = 1.5046, B = 0.00420 (in micrometers squared)
        double A = 1.5046;
        double B = 0.00420;
        return A + (B / (lambdaMicrometers * lambdaMicrometers));
    }
}

public class DiffuseLight(ITexture tex) : Material
{
    public DiffuseLight(ColorRGB emit) : this(new SolidColor(emit)) { }
    public override double Emitted(Ray rIn, HitRecord rec, double u, double v, Vector3d p)
    {
        // Light is only emitted on the front face
        if (!rec.FrontFace) return 0.0;
        return Tex.Value(u, v, p, rIn.Wavelength);
    }
    protected ITexture Tex = tex;
}

public class Isotropic(ITexture tex) : Material
{
    public Isotropic(ColorRGB albedo) : this(new SolidColor(albedo)) { }
    protected ITexture Tex = tex;
    public override bool Scatter(Ray rIn, HitRecord rec, ScatterRecord sRec, RTRandom generator)
    {
        sRec.Attenuation = Tex.Value(rec.U, rec.V, rec.P, rIn.Wavelength);
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
    public double Attenuation; // At the given wavelength
    public PDF? SourcePDF;
    public bool SkipPDF;
    public Ray? SkipPDFRay;
}