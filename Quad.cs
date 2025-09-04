using System.Runtime.CompilerServices;

namespace RayTrace;

public class Quad : Hittable
{
    public Quad(Vector3d q, Vector3d u, Vector3d v, Material m, string name = "Quad")
    {
        Q = q;
        U = u;
        V = v;
        Mat = m;
        Vector3d n = Vector3d.Cross(U, V);
        Normal = n.UnitVector;
        D = Vector3d.Dot(Normal, Q);
        W = n / Vector3d.Dot(n, n);
        Area = n.Length;
        SetBoundingBox(new AABB(new AABB(q, q + u + v), new AABB(q + u, q + v)));
        SetName(name);
    }

    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        double denominator = Vector3d.Dot(Normal, r.Direction);
        // No hit if the ray is parallel to the plane
        if (Math.Abs(denominator) < 1.0e-8) return false;
        // Check if the hit point parameter t is outside the ray interval
        // Reminder: t is the distance along the ray from the origin
        double t = (D - Vector3d.Dot(Normal, r.Origin)) / denominator;
        if (!rayT.Contains(t)) return false;
        Vector3d intersection = r.At(t);
        // Determine if the hit point lies within the planar shape using its plane coordinates
        Vector3d planarHitPointVector = intersection - Q;
        double alpha = Vector3d.Dot(W, Vector3d.Cross(planarHitPointVector, V));
        double beta = Vector3d.Dot(W, Vector3d.Cross(U, planarHitPointVector));
        // Checking the interior updates rec.U and rec.V
        if (!IsInterior(alpha, beta, rec)) return false;
        // Update the hit record - the ray hit the 2D shape
        rec.T = t;
        rec.P = intersection;
        rec.Mat = Mat;
        rec.SetFaceNormal(r, Normal);
        return true;
    }

    protected virtual bool IsInterior(double alpha, double beta, HitRecord rec)
    {
        Interval unitInterval = new(0.0, 1.0);
        // Having been given the hit point in the plane's coordinates, return false
        // if it is outside the primitive - otherwise set the hit record UV coordinates
        // and return true
        if (!unitInterval.Contains(alpha) || !unitInterval.Contains(beta)) return false;
        rec.U = alpha;
        rec.V = beta;
        return true;
    }
    private Material Mat;
    private Vector3d U, V, Q, W;
    private Vector3d Normal;
    private double D; // Locates the plane
    private double Area;

    public static HittableList Box(Vector3d a, Vector3d b, Material mat)
    {
        // Returns a 3D box of six sides defined by two opposite corners, a and b
        HittableList sides = new();

        Vector3d min = new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        Vector3d max = new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));

        Vector3d dx = new(max.X - min.X, 0, 0);
        Vector3d dy = new(0, max.Y - min.Y, 0);
        Vector3d dz = new(0, 0, max.Z - min.Z);

        sides.Add(new Quad(new Vector3d(min.X, min.Y, max.Z), dx, dy, mat, "Front"));
        sides.Add(new Quad(new Vector3d(max.X, min.Y, max.Z), -dz, dy, mat, "Right"));
        sides.Add(new Quad(new Vector3d(max.X, min.Y, min.Z), -dx, dy, mat, "Back"));
        sides.Add(new Quad(new Vector3d(min.X, min.Y, min.Z), dz, dy, mat, "Left"));
        sides.Add(new Quad(new Vector3d(min.X, max.Y, max.Z), dx, -dz, mat, "Top"));
        sides.Add(new Quad(new Vector3d(min.X, min.Y, min.Z), dx, dz, mat, "Bottom"));

        return sides;
    }

    public override double PDFValue(Vector3d origin, Vector3d direction)
    {
        HitRecord rec = new();
        if (!Hit(new Ray(origin, direction), new Interval(0.001, double.PositiveInfinity), rec))
            return 0.0;

        double distanceSquared = rec.T * rec.T * direction.LengthSquared;
        double cosine = Math.Abs(Vector3d.Dot(direction, rec.Normal) / direction.Length);
        return distanceSquared / (cosine * Area);
    }
    
    public override Vector3d Random(Vector3d origin, RTRandom generator)
    {
        Vector3d randomPoint = Q + generator.RandomDouble() * U + generator.RandomDouble() * V;
        return randomPoint - origin;
    }
}