namespace RayTrace;
// * BVHNode

public class BVHNode : Hittable
{
    private void PrintObjects(int depth)
    {
        if (Left is BVHNode leftNode)
        {
            for (int i = 0; i < depth + 2; i++)
            {
                Console.Write("-");
            }
            Console.Write("> ");
            leftNode.PrintObjects(depth + 1);
        }
        else
        {
            Console.Write($"L{depth:D4}: {Left.GetName()}, ");
        }
        if (Right is BVHNode rightNode)
        {
            for (int i = 0; i < depth + 2; i++)
            {
                Console.Write("-");
            }
            Console.Write("> ");
            rightNode.PrintObjects(depth + 1);
        }
        else
        {
            Console.Write($"R{depth:D4}: {Right.GetName()}, ");
        }
        Console.Write("\n");
    }
    private Hittable Left, Right;
    public BVHNode(HittableList hitList) : this(hitList.Objects, 0, hitList.Objects.Count) { }
    public BVHNode(LinkedList<Hittable> objects, int start, int end, int depth = 0, bool isLeft = true)
    {
        // Build the bounding box of the span of the source objects
        boundingBox = AABB.empty;
        for (int objectIndex = start; objectIndex < end; objectIndex++)
        {
            boundingBox = new AABB(boundingBox, objects.ElementAt(objectIndex).GetBoundingBox());
        }
        // Return 0, 1, or 2
        int axis = boundingBox.LongestAxis;
        // Which comparator to use?
        int comparator(Hittable a, Hittable b) => (axis == 0) ? BoxXCompare(a, b)
                                                : (axis == 1) ? BoxYCompare(a, b)
                                                : BoxZCompare(a, b);
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
            Left = new BVHNode(leftList, 0, leftList.Count, depth + 1, true);
            Right = new BVHNode(rightList, 0, rightList.Count, depth + 1, false);
        }
        SetBoundingBox(new AABB(Left.GetBoundingBox(), Right.GetBoundingBox()));
        SetName($"BVHNode_{depth}{(isLeft ? "L" : "R")}");
    }
    public override bool Hit(Ray r, Interval rayT, HitRecord rec)
    {
        // If the ray doesn't hit our enclosing bounding box, no need to check further
        #if SINGLERAY
        Console.WriteLine($"BVHNode {GetName()}: Testing bounding box {GetBoundingBox()} against interval {rayT}");
        #endif
        if (!GetBoundingBox().Hit(r, rayT))
        {
            #if SINGLERAY
            Console.WriteLine($"BVHNode {GetName()}: Missed bounding box {GetBoundingBox()} checked against interval {rayT}");
            #endif
            return false;
        }
        // Check for hits in the left and right subtrees
        #if SINGLERAY
        Console.WriteLine($"BVHNode {GetName()}: Checking L={Left.GetName()}, R={Right.GetName()}");
        #endif
        bool hitLeft = Left.Hit(r, rayT, rec);
        // If Left hits, only check Right up to the point of that hit << rayT.Min may have been overwritten!
        bool hitRight = Right.Hit(r, new Interval(rayT.Min, hitLeft ? rec.T : rayT.Max), rec);
        #if SINGLERAY
        Console.WriteLine($"BVHNode {GetName()} containing:");
        PrintObjects(0);
        Console.Write($" --> {GetName()} result: LN {Left.GetName()}={hitLeft}; RN {Right.GetName()}={hitRight}\n");
        #endif
        return hitLeft || hitRight;
    }
    private string HitName = "BVHNode";
    public override string GetName()
    {
        return HitName;
    }
    public override void SetName(string name)
    {
        HitName = name;
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