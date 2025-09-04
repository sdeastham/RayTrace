namespace RayTrace;

public class OrthoNormalBasis
{
    public Vector3d[] Axis { get; private set; } = new Vector3d[3];
    public Vector3d U { get { return Axis[0]; } private set { Axis[0] = value; } }
    public Vector3d V { get { return Axis[1]; } private set { Axis[1] = value; } }
    public Vector3d W { get { return Axis[2]; } private set { Axis[2] = value; } }

    public OrthoNormalBasis(Vector3d n)
    {
        W = n.UnitVector;
        Vector3d a = Math.Abs(W.X) > 0.9 ? new Vector3d(0.0, 1.0, 0.0) : new Vector3d(1.0, 0.0, 0.0);
        V = Vector3d.Cross(W, a).UnitVector;
        U = Vector3d.Cross(W, V);
    }

    public Vector3d Transform(double a, double b, double c)
    {
        return a * U + b * V + c * W;
    }

    public Vector3d Transform(Vector3d a)
    {
        return a.X * U + a.Y * V + a.Z * W;
    }
}