using System.Reflection.Metadata.Ecma335;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RayTrace;

public class Color(double r, double g, double b) : Vector3d(r, g, b)
{
    public static readonly Color black = new(0, 0, 0);
    public static readonly Color white = new(1, 1, 1);
    public double R => Data[0];
    public double G => Data[1];
    public double B => Data[2];

    public static Color operator *(Color c, Vector3d v) => new(c.R * v.X, c.G * v.Y, c.B * v.Z);
    public static Color operator *(Color c, double d) => new(c.R * d, c.G * d, c.B * d);
}

public interface ITexture
{
    Color Value(double u, double v, Vector3d p);
}

public class Texture : ITexture
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
        // Texture is actually checkered in space; all that matters is where the hit occurred in 3D space
        int xInteger = (int)Math.Floor(InvScale * p.X);
        int yInteger = (int)Math.Floor(InvScale * p.Y);
        int zInteger = (int)Math.Floor(InvScale * p.Z);
        return (xInteger + yInteger + zInteger) % 2 == 0 ? Odd.Value(u, v, p) : Even.Value(u, v, p);
    }
}

public class ImageTexture(Image img) : Texture
{

    public ImageTexture(string filename) : this(Image.Load(filename)) { }
    private Image<Rgba32> ImageData = img.CloneAs<Rgba32>();

    public override Color Value(double u, double v, Vector3d p)
    {
        // If we have no image, return solid cyan as a debugging aid
        if (ImageData == null)
        {
            return new Color(0, 1, 1);
        }

        // Clamp input texture coordinates to [0,1] x [1,0]
        u = Math.Clamp(u, 0.0, 1.0);
        v = 1.0 - Math.Clamp(v, 0.0, 1.0); // Flip V to image coordinates

        int i = (int)(u * ImageData.Width);
        int j = (int)(v * ImageData.Height);

        // Clamp integer mapping, since actual coordinates should be less than 1.0
        if (i >= ImageData.Width) i = ImageData.Width - 1;
        if (j >= ImageData.Height) j = ImageData.Height - 1;

        // Likely very inefficient access method
        var pixel = ImageData[i, j].ToVector4();
        double r = pixel.X;
        double g = pixel.Y;
        double b = pixel.Z;
        return new Color(r, g, b);
    }
}

class NoiseTexture(double scale) : Texture
{
    // Texture with Perlin noise. The larger the value of scale,
    // the higher-frequency the noise.
    private readonly Perlin Noise = new();
    private readonly double Scale = scale;
    public override Color Value(double u, double v, Vector3d p)
    {
        return new Color(1.0, 1.0, 1.0) * 0.5 * (1.0 + Noise.Noise(Scale * p));
    }
}