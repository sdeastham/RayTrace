namespace RayTrace;
// * BVHNode

public class BVHNode : Hittable
{
    private Hittable Left, Right;
    public BVHNode(HittableList hitList) : this(hitList.Objects, 0, hitList.Objects.Count) { }
    public BVHNode(LinkedList<Hittable> objects, int start, int end)
    {
        // Return 0, 1, or 2
        int axis = Random.Shared.Next(0, 3);
        // Which comparator to use?
        int comparator(Hittable a, Hittable b) => (axis == 0) ? BoxXCompare(a, b)
                                                : (axis == 1) ? BoxYCompare(a, b)
                                                :               BoxZCompare(a, b);
        // How many objects are enclosed?
        int objectSpan = end - start;
        if (objectSpan == 1)
        {
            // Only one object - copy to both subtrees
            Left = Right = objects.ElementAt(start);
        }
        else if (objectSpan == 2)
        {
            // Two objects - create leaf nodes for each
            Left = objects.ElementAt(start);
            Right = objects.ElementAt(start + 1);
        }
        else
        {
            // More than two objects - split them between the subtrees
            var sortedObjects = objects.ToList();
            sortedObjects.Sort(comparator);
            var mid = start + objectSpan / 2;
            // Does this return the correct thing?
            var leftList = new LinkedList<Hittable>(sortedObjects.GetRange(start, objectSpan / 2));
            var rightList = new LinkedList<Hittable>(sortedObjects.GetRange(mid, objectSpan - objectSpan / 2));
            Left = new BVHNode(leftList, 0, leftList.Count);
            Right = new BVHNode(rightList, 0, rightList.Count);
        }
        SetBoundingBox(new AABB(Left.GetBoundingBox(), Right.GetBoundingBox()));
    }
    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        if (!GetBoundingBox().Hit(r, rayT)) return false;
        bool hitLeft = Left.Hit(r, rayT, rec);
        bool hitRight = Right.Hit(r, new Interval(rayT.Min, hitLeft ? rec.T : rayT.Max), rec);
        return hitLeft || hitRight;
    }
    private AABB boundingBox;

    public override AABB GetBoundingBox()
    {
        return boundingBox;
    }

    public override void SetBoundingBox(AABB value)
    {
        boundingBox = value;
    }

    static int BoxCompare(Hittable a, Hittable b, int axisIndex)
    {
        var aAxisInterval = a.GetBoundingBox().AxisInterval(axisIndex);
        var bAxisInterval = b.GetBoundingBox().AxisInterval(axisIndex);
        // Double check if this is the right way around!
        return aAxisInterval.Min < bAxisInterval.Min ? -1 : 1;
    }

    static int BoxXCompare(Hittable a, Hittable b) => BoxCompare(a, b, 0);
    static int BoxYCompare(Hittable a, Hittable b) => BoxCompare(a, b, 1);
    static int BoxZCompare(Hittable a, Hittable b) => BoxCompare(a, b, 2);
}