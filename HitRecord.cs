namespace RayTrace;

public class HitRecord
{
    // Tracks the most recent interaction between a ray and a surface
    public Vector3d P, Normal;
    public double T;
    public Material? Mat;
    public double U, V; // Coordinates of the hit on the object

    public HitRecord(Vector3d p, Vector3d normal, double t)
    {
        P = p;
        Normal = normal;
        T = t;
        Mat = null;
        U = 0.0;
        V = 0.0;
    }

    public HitRecord() : this(new Vector3d(0.0,0.0,0.0), new Vector3d(1.0,0.0,0.0), double.PositiveInfinity) { }

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
        U = rec.U;
        V = rec.V;
    }
}