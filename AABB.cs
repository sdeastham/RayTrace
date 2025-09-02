namespace RayTrace;

public class AABB
{
    public Interval XInterval, YInterval, ZInterval;

    public AABB(Interval xInterval, Interval yInterval, Interval zInterval)
    {
        XInterval = xInterval;
        YInterval = yInterval;
        ZInterval = zInterval;
    }

    public AABB(Vector3d a, Vector3d b)
    {
        XInterval = (a.X <= b.X) ? new Interval(a.X, b.X) : new Interval(b.X, a.X);
        YInterval = (a.Y <= b.Y) ? new Interval(a.Y, b.Y) : new Interval(b.Y, a.Y);
        ZInterval = (a.Z <= b.Z) ? new Interval(a.Z, b.Z) : new Interval(b.Z, a.Z);
    }

    public AABB(AABB box0, AABB box1)
    {
        XInterval = new(box0.XInterval, box1.XInterval);
        YInterval = new(box0.YInterval, box1.YInterval);
        ZInterval = new(box0.ZInterval, box1.ZInterval);
    }

    // Default AABB is empty, using the default (empty) intervals
    public AABB()
    {
        XInterval = new();
        YInterval = new();
        ZInterval = new();
    }

    public Interval AxisInterval(int n)
    {
        if (n == 1) return YInterval;
        if (n == 2) return ZInterval;
        return XInterval;
    }

    public bool Hit(Ray r, Interval rayT)
    {
        Vector3d rayOrigin = r.Origin;
        Vector3d rayDirection = r.Direction;
        for (int axis = 0; axis < 3; axis++)
        {
            // Bounds of the slab in this dimension
            Interval ax = AxisInterval(axis);
            double adInv = 1.0 / rayDirection.Data[axis];
            // Coordinate of the intersection of the ray with the interval minimum, 
            // in terms of distance along the ray from the origin in the direction of
            // travel of the ray. t1 is then the intersection with the maximum.
            double t0 = (ax.Min - rayOrigin.Data[axis]) * adInv;
            double t1 = (ax.Max - rayOrigin.Data[axis]) * adInv;
            double mintersect = rayT.Min;
            double maxtersect = rayT.Max;
            if (t0 < t1)
            {
                // Direction of travel of the ray is positive in this dimension
                if (t0 > mintersect) mintersect = t0;
                if (t1 < maxtersect) maxtersect = t1;
            }
            else
            {
                // Direction of travel of the ray is negative in this dimension
                if (t1 > mintersect) mintersect = t1;
                if (t0 < maxtersect) maxtersect = t0;
            }
            if (maxtersect <= mintersect) return false;
        }
        return true;
    }
}
