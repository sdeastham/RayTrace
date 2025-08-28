using System.Numerics;

namespace RayTrace;

// Holds:
// * Hittable     - abstract class of hittable objects
// * HittableList - LinkedList which holds all the Hittables which have interacted with a Ray
// * Sphere       - implementation of Hittable for a sphere

public class HittableList : Hittable
{
    public LinkedList<Hittable> Objects;

    public HittableList()
    {
        Objects = new LinkedList<Hittable>();
    }

    public HittableList(Hittable Object)
    {
        Objects = new LinkedList<Hittable>([Object]);
    }

    public void Clear() => Objects.Clear();

    public void Add(Hittable Object) => Objects.AddLast(Object);

    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        HitRecord tempRec = new();
        bool hitAnything = false;
        var closestSoFar = rayT.Max;
        foreach (Hittable tempObject in Objects)
        {
            if (tempObject.Hit(r, new Interval(rayT.Min, closestSoFar), tempRec))
            {
                hitAnything = true;
                closestSoFar = (float)tempRec.T;
                // Update the "closest" hit
                rec.Overwrite(tempRec);
            }
        }
        return hitAnything;
    }
}

public class HitRecord
{
    // Tracks the most recent interaction between a ray and a surface
    public Vector3d P, Normal;
    public double T;
    public Material? Mat;

    public HitRecord(Vector3d p, Vector3d normal, double t)
    {
        P = p;
        Normal = normal;
        T = t;
        Mat = null;
    }

    public HitRecord()
    {
        // Default constructor - 
        P = new Vector3d(0.0, 0.0, 0.0);
        Normal = new Vector3d(1.0, 0.0, 0.0);
        T = double.PositiveInfinity;
        Mat = null;
    }

    public bool FrontFace = false;

    public void SetFaceNormal(Ray r, Vector3d outwardNormal)
    {
        // Sets the hit record normal vector
        // Parameter outwardNormal is assumed to have unit length
        FrontFace = Vector3d.Dot(r.Direction, outwardNormal) < 0.0f;
        // If the ray comes from outside
        Normal = FrontFace ? outwardNormal : -outwardNormal;
    }

    public void Overwrite(HitRecord rec)
    {
        P = rec.P;
        T = rec.T;
        Mat = rec.Mat;
        Normal = rec.Normal;
        FrontFace = rec.FrontFace;
    }
}

public abstract class Hittable
{
    public abstract bool Hit(Ray r, Interval rayT, HitRecord rec);
}

public class Sphere(Vector3d center, double radius, Material mat) : Hittable
{
    public Vector3d Center { get; private set; } = center;
    public double Radius { get; private set; } = Math.Max(0.0,radius);
    public Material Mat { get; private set; } = mat;

    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        Vector3d oc = Center - r.Origin;
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
        Vector3d outwardNormal = (rec.P - Center) / Radius;
        rec.Mat = Mat;
        rec.SetFaceNormal(r, outwardNormal);
        return true;
    }
}