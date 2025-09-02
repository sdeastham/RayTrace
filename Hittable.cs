using System.Numerics;

namespace RayTrace;

// Holds:
// * Hittable     - abstract class of hittable objects
// * HittableList - LinkedList which holds all the Hittables which have interacted with a Ray
// * Sphere       - implementation of Hittable for a sphere
// * HitRecord


public class HittableList : Hittable
{
    private AABB boundingBox = new();
    private string HitName = "HittableList";
    public override string GetName()
    {
        return HitName;
    }
    public override void SetName(string name)
    {
        HitName = name;
    }

    public override AABB GetBoundingBox()
    {
        return boundingBox;
    }

    public override void SetBoundingBox(AABB value)
    {
        boundingBox = value;
    }

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

    public void Add(Hittable Object)
    {
        Objects.AddLast(Object);
        // Expand the bounding box to cover the new object
        SetBoundingBox(new AABB(GetBoundingBox(), Object.GetBoundingBox()));
    }

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

public interface IHittable
{
    AABB GetBoundingBox();
    bool Hit(Ray r, Interval rayT, HitRecord rec);
    void SetBoundingBox(AABB value);
    string GetName();
}

public abstract class Hittable : IHittable
{
    public abstract bool Hit(Ray r, Interval rayT, HitRecord rec);
    public abstract AABB GetBoundingBox();
    public abstract void SetBoundingBox(AABB value);
    public abstract string GetName();
    public abstract void SetName(string name);
}

public class Sphere : Hittable
{
    public Ray Center { get; private set; }
    public double Radius { get; private set; }
    public Material Mat { get; private set; }
    private AABB boundingBox;
    private string HitName = "Sphere";

    public override AABB GetBoundingBox()
    {
        return boundingBox;
    }

    public override void SetName(string name)
    {
        HitName = name;
    }

    public override string GetName()
    {
        return HitName;
    }

    public override void SetBoundingBox(AABB value)
    {
        boundingBox = value;
    }

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
        return true;
    }
}