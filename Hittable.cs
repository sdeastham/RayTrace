using System.Numerics;

namespace RayTrace;

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

    public override bool Hit(Ray r, float rayTMin, float rayTMax, HitRecord rec)
    {
        HitRecord tempRec = new();
        bool hitAnything = false;
        var closestSoFar = rayTMax;
        foreach (Hittable tempObject in Objects)
        {
            if (tempObject.Hit(r, rayTMin, closestSoFar, tempRec))
            {
                hitAnything = true;
                closestSoFar = (float)tempRec.T;
                //rec = tempRec;
                rec.T = tempRec.T;
                rec.P = tempRec.P;
                rec.Normal = tempRec.Normal;
            }
        }
        return hitAnything;
    }
}

public class HitRecord
{
    // Tracks the most recent interaction between a ray and a surface
    public Vector3 P, Normal;
    public float T;

    public HitRecord(Vector3 p, Vector3 normal, float t)
    {
        P = p;
        Normal = normal;
        T = t;
    }

    public HitRecord()
    {
        // Default constructor - 
        P = new Vector3(0.0f, 0.0f, 0.0f);
        Normal = new Vector3(1.0f, 0.0f, 0.0f);
        T = float.PositiveInfinity;
    }

    public bool FrontFace = false;

    public void SetFaceNormal(Ray r, Vector3 outwardNormal)
    {
        // Sets the hit record normal vector
        // Parameter outwardNormal is assumed to have unit length
        FrontFace = Vector3.Dot(r.Direction, outwardNormal) < 0.0f;
        // If the ray comes from outside
        Normal = FrontFace ? outwardNormal : -outwardNormal;
    }
}

public abstract class Hittable
{
    public abstract bool Hit(Ray r, float rayTMin, float rayTMax, HitRecord rec);
}

public class Sphere(Vector3 center, float radius) : Hittable
{
    public Vector3 Center { get; private set; } = center;
    public float Radius { get; private set; } = Math.Max(0.0f,radius);

    public override bool Hit(Ray r, float rayTMin, float rayTMax, HitRecord rec)
    {
        Vector3 oc = Center - r.Origin;
        var a = r.Direction.LengthSquared();
        var h = Vector3.Dot(r.Direction, oc);
        var c = oc.LengthSquared() - Radius * Radius;
        var discriminant = h * h - a * c;
        if (discriminant < 0)
        {
            return false;
        }
        var sqrtd = Math.Sqrt(discriminant);
        // Find the nearest root within the valid range
        var root = (h - sqrtd) / a;
        if (root <= rayTMin || rayTMax <= root)
        {
            root = (h + sqrtd) / a;
            if (root <= rayTMin || rayTMax <= root)
            {
                return false;
            }
        }
        rec.T = (float)root;
        rec.P = r.At(rec.T);
        Vector3 outwardNormal = (rec.P - Center) / Radius;
        rec.SetFaceNormal(r, outwardNormal);
        return true;
    }
}