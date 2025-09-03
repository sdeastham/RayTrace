using System.Runtime.CompilerServices;

namespace RayTrace;

public class Quad : Hittable
{
    public Quad(Vector3d q, Vector3d u, Vector3d v, Material m, string name="Quad")
    {
        Q = q;
        U = u;
        V = v;
        Mat = m;
        Vector3d n = Vector3d.Cross(U, V);
        Normal = n.UnitVector;
        D = Vector3d.Dot(Normal, Q);
        W = n / Vector3d.Dot(n, n);
        SetBoundingBox(new AABB(new AABB(q, q + u + v), new AABB(q + u, q + v)));
        SetName(name);
    }
    public override void SetName(string name) => Name = name;
    public override string GetName() => Name;
    public override void SetBoundingBox(AABB bbox)
    {
        BoundingBox = bbox;
    }

    public override AABB GetBoundingBox()
    {
        return BoundingBox;
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

    private AABB BoundingBox;
    private Material Mat;
    private Vector3d U, V, Q, W;
    private Vector3d Normal;
    private double D; // Locates the plane
    private string Name;
}