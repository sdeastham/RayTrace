namespace RayTrace;

public class Sphere : Hittable
{
    public Ray Center { get; private set; }
    public double Radius { get; private set; }
    public Material Mat { get; private set; }

    public Sphere(Vector3d center, double radius, Material mat, string name = "Sphere")
    {
        // Stationary sphere
        Center = new(center, new Vector3d(0, 0, 0));
        Radius = Math.Max(0.0, radius);
        Mat = mat;
        SetName(name);
        Vector3d rVec = new(radius, radius, radius);
        SetBoundingBox(new AABB(center - rVec, center + rVec));
    }

    public Sphere(Vector3d center1, Vector3d center2, double radius, Material mat, string name = "MovingSphere")
    {
        // Moving sphere
        Center = new(center1, center2 - center1);
        Radius = Math.Max(0.0, radius);
        Mat = mat;
        SetName(name);
        Vector3d rVec = new(radius, radius, radius);
        AABB box1 = new(Center.At(0.0) - rVec, Center.At(0.0) + rVec);
        AABB box2 = new(Center.At(1.0) - rVec, Center.At(1.0) + rVec);
        SetBoundingBox(new(box1, box2));
    }

    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        //Console.WriteLine($"Sphere {GetName()}: Testing against interval {rayT}");
        Vector3d currentCenter = Center.At(r.Time);
        Vector3d oc = currentCenter - r.Origin;
        var a = r.Direction.LengthSquared;
        var h = Vector3d.Dot(r.Direction, oc);
        var c = oc.LengthSquared - Radius * Radius;
        var discriminant = h * h - a * c;
        if (discriminant < 0)
        {
            return false;
        }
        var sqrtd = Math.Sqrt(discriminant);
        // Find the nearest root within the valid range
        var root = (h - sqrtd) / a;
        if (!rayT.Surrounds(root))
        {
            root = (h + sqrtd) / a;
            if (!rayT.Surrounds(root))
            {
                return false;
            }
        }
        rec.T = root;
        rec.P = r.At(rec.T);
        Vector3d outwardNormal = (rec.P - currentCenter) / Radius;
        rec.Mat = Mat;
        rec.SetFaceNormal(r, outwardNormal);
        (rec.U, rec.V) = GetSphereUV(outwardNormal);
        return true;
    }

    public static (double, double) GetSphereUV(Vector3d p)
    {
        double theta = Math.Acos(-p.Y);
        double phi = Math.Atan2(-p.Z, p.X) + Math.PI;

        double u = phi / (2 * Math.PI);
        double v = theta / Math.PI;
        return (u, v);
    }

    public override double PDFValue(Vector3d origin, Vector3d direction)
    {
        HitRecord rec = new();
        if (!Hit(new Ray(origin, direction), new Interval(0.001, double.PositiveInfinity), rec))
            return 0.0;
        double distanceSquared = (Center.At(0) - origin).LengthSquared;
        double cosThetaMax = Math.Sqrt(1 - Radius * Radius / distanceSquared);
        double solidAngle = 2.0 * Math.PI * (1.0 - cosThetaMax);
        return 1.0 / solidAngle;
    }

    public override Vector3d Random(Vector3d origin, RTRandom generator)
    {
        Vector3d direction = Center.At(0) - origin;
        double distanceSquared = direction.LengthSquared;
        OrthoNormalBasis uvw = new(direction);
        return uvw.Transform(RandomToSphere(Radius, distanceSquared, generator));
    }

    private static Vector3d RandomToSphere(double radius, double distanceSquared, RTRandom generator)
    {
        double r1 = generator.RandomDouble();
        double r2 = generator.RandomDouble();
        double z = 1 + r2 * (Math.Sqrt(1 - radius * radius / distanceSquared) - 1);

        double phi = 2 * Math.PI * r1;
        double x = Math.Cos(phi) * Math.Sqrt(1 - z * z);
        double y = Math.Sin(phi) * Math.Sqrt(1 - z * z);

        return new Vector3d(x, y, z);
    }
}