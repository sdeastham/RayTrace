namespace RayTrace;

public class Color : Vector3d
{
    public Color(double r, double g, double b) : base(r, g, b) { }

    public static readonly Color black = new Color(0, 0, 0);
    public static readonly Color white = new Color(1, 1, 1);
    public double R => Data[0];
    public double G => Data[1];
    public double B => Data[2];
}

public class Texture
{
    public virtual Color Value(double u, double v, Vector3d p)
    {
        return Color.black;
    }
}

public class SolidColor : Texture
{
    private Color Albedo;
    public SolidColor(Color albedo)
    {
        Albedo = albedo;
    }
    public SolidColor(double r, double g, double b) : this(new Color(r, g, b)) { }
    public override Color Value(double u, double v, Vector3d p)
    {
        return Albedo;
    }
}

public class CheckerTexture : Texture
{
    private double InvScale;
    private Texture Odd, Even;

    public CheckerTexture(double scale, Texture odd, Texture even)
    {
        InvScale = 1.0 / scale;
        Odd = odd;
        Even = even;
    }

    public CheckerTexture(double scale, Color odd, Color even) : this(scale, new SolidColor(odd), new SolidColor(even)) { }

    public override Color Value(double u, double v, Vector3d p)
    {
        // Compute the checkerboard pattern
        //double sines = Math.Sin(InvScale * u) * Math.Sin(InvScale * v);
        //return sines < 0 ? Odd.Value(u, v, p) : Even.Value(u, v, p);
        int xInteger = (int)Math.Floor(InvScale * p.X);
        int yInteger = (int)Math.Floor(InvScale * p.Y);
        int zInteger = (int)Math.Floor(InvScale * p.Z);
        return (xInteger + yInteger + zInteger) % 2 == 0 ? Odd.Value(u, v, p) : Even.Value(u, v, p);
    }
}