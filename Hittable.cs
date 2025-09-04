using System.Numerics;

namespace RayTrace;

// Holds:
// * Hittable     - abstract class of hittable objects
// * HittableList - LinkedList which holds all the Hittables which have interacted with a Ray
// * Sphere       - implementation of Hittable for a sphere
// * HitRecord


public class HittableList : Hittable
{
    protected new string Name = "HittableList";
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

public interface IHittable
{
    AABB GetBoundingBox();
    bool Hit(Ray r, Interval rayT, HitRecord rec);
    void SetBoundingBox(AABB value);
    string GetName();
    void SetName(string name);
    Vector3d Random(Vector3d origin, RTRandom generator);
    double PDFValue(Vector3d origin, Vector3d direction);
}

public abstract class Hittable : IHittable
{
    public abstract bool Hit(Ray r, Interval rayT, HitRecord rec);
    public virtual AABB GetBoundingBox() => BoundingBox;
    public virtual void SetBoundingBox(AABB value) => BoundingBox = value;
    public virtual void SetName(string name) => Name = name;
    public virtual string GetName() => Name;
    protected AABB BoundingBox = new();
    public string Name = "Hittable"; // Default name
    public virtual Vector3d Random(Vector3d origin, RTRandom generator)
    {
        return new Vector3d(1.0, 0.0, 0.0);
    }
    public virtual double PDFValue(Vector3d origin, Vector3d direction)
    {
        return 0.0;
    }
}

public class Translate : Hittable
{
    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        // Move the ray backwards by the offset
        Ray offsetRay = new(r.Origin - Offset, r.Direction, r.Time);
        // Determine whether an intersection exists along the offset ray and, if so, where
        if (!Object.Hit(offsetRay, rayT, rec)) return false;
        // Move the intersection point forwards by the offset
        rec.P += Offset;
        return true;
    }
    public Translate(Hittable obj, Vector3d displacement)
    {
        Object = obj;
        Offset = displacement;
        SetBoundingBox(obj.GetBoundingBox() + Offset);
        SetName("Translate(" + obj.GetName() + ")");
    }
    public override AABB GetBoundingBox() => Object.GetBoundingBox();
    private Hittable Object;
    private Vector3d Offset;
}

public class RotateY : Hittable
{
    public RotateY(Hittable obj, double angle)
    {
        double radians = angle * Math.PI / 180.0;
        CosTheta = Math.Cos(radians);
        SinTheta = Math.Sin(radians);
        Object = obj;
        SetBoundingBox(obj.GetBoundingBox());
        SetName("RotateY(" + obj.GetName() + ")");
        Vector3d min = new(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
        Vector3d max = new(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    double x = i * BoundingBox.XInterval.Max + (1 - i) * BoundingBox.XInterval.Min;
                    double y = j * BoundingBox.YInterval.Max + (1 - j) * BoundingBox.YInterval.Min;
                    double z = k * BoundingBox.ZInterval.Max + (1 - k) * BoundingBox.ZInterval.Min;
                    double newX = CosTheta * x + SinTheta * z;
                    double newZ = -SinTheta * x + CosTheta * z;
                    Vector3d tester = new(newX, y, newZ);
                    for (int c = 0; c < 3; c++)
                    {
                        if (tester.Data[c] > max.Data[c]) max.Data[c] = tester.Data[c];
                        if (tester.Data[c] < min.Data[c]) min.Data[c] = tester.Data[c];
                    }
                }
            }
        }
    }
    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        // Transform the ray from world space to the object's local space
        Vector3d origin = new(CosTheta * r.Origin.X - SinTheta * r.Origin.Z,
                              r.Origin.Y,
                              SinTheta * r.Origin.X + CosTheta * r.Origin.Z);
        Vector3d direction = new(CosTheta * r.Direction.X - SinTheta * r.Direction.Z,
                                 r.Direction.Y,
                                 SinTheta * r.Direction.X + CosTheta * r.Direction.Z);
        Ray rotatedRay = new(origin, direction, r.Time);
        // Determine whether an intersection exists along the rotated ray and, if so, where
        if (!Object.Hit(rotatedRay, rayT, rec)) return false;
        // Transform the intersection point and normal from local space back to world space
        rec.P = new(CosTheta * rec.P.X + SinTheta * rec.P.Z,
                    rec.P.Y,
                    -SinTheta * rec.P.X + CosTheta * rec.P.Z);
        rec.Normal = new(CosTheta * rec.Normal.X + SinTheta * rec.Normal.Z,
                         rec.Normal.Y,
                         -SinTheta * rec.Normal.X + CosTheta * rec.Normal.Z);
        return true;
    }

    public override AABB GetBoundingBox() => Object.GetBoundingBox();
    private double CosTheta, SinTheta;
    private Hittable Object;
}