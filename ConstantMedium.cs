using System.Diagnostics;

namespace RayTrace;

public class ConstantMedium : Hittable
{
    public ConstantMedium(Hittable boundary, double density, Texture texture, string name = "ConstantMedium")
    {
        Boundary = boundary;
        Density = density;
        PhaseFunction = new Isotropic(texture);
        SetBoundingBox(Boundary.GetBoundingBox());
        Name = name;
    }

    public ConstantMedium(Hittable boundary, double density, Color color, string name = "ConstantMedium")
    {
        Boundary = boundary;
        Density = density;
        NegativeInverseDensity = -1.0 / density;
        PhaseFunction = new Isotropic(color);
        SetBoundingBox(Boundary.GetBoundingBox());
        Name = name;
    }

    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        double minterval = rayT.Min;
        double maxterval = rayT.Max;
        HitRecord rec1 = new(), rec2 = new();
        if (!Boundary.Hit(r, Interval.universe, rec1)) return false;
        if (!Boundary.Hit(r, new Interval(rec1.T + 1.0e-8, double.PositiveInfinity), rec2)) return false;
        if (rec1.T < minterval) rec1.T = minterval;
        if (rec2.T > maxterval) rec2.T = maxterval;
        if (rec1.T >= rec2.T) return false;
        if (rec1.T < 0) rec1.T = 0;
        double rayLength = r.Direction.Length;
        double distanceInsideBoundary = (rec2.T - rec1.T) * rayLength;
        double hitDistance = -(1.0 / Density) * Math.Log(Generator.RandomDouble());
        if (hitDistance > distanceInsideBoundary) return false;
        rec.T = rec1.T + hitDistance / rayLength;
        rec.P = r.At(rec.T);
        rec.Normal = new(1.0, 0.0, 0.0); // Arbitrary
        rec.FrontFace = true; // Also arbitrary
        rec.Mat = PhaseFunction;
        return true;
    }
    // We don't actually want our own bounding box - we want to use that
    // of our child object directly
    public override AABB GetBoundingBox() => Boundary.GetBoundingBox();

    private Hittable Boundary;
    private double Density;
    private double NegativeInverseDensity;
    private Material PhaseFunction;
    private RTRandom Generator = new();
}