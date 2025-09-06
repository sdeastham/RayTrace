using System.Reflection.Metadata.Ecma335;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RayTrace;

public interface ITexture
{
    double Value(double u, double v, Vector3d p, double wavelength);
}

public class Texture : ITexture
{
    public virtual double Value(double u, double v, Vector3d p, double wavelength)
    {
        return 0.0;
    }
}

public class SolidColor : Texture
{
    private ColorRGB Albedo;
    public SolidColor(ColorRGB albedo)
    {
        Albedo = albedo;
    }
    public SolidColor(double r, double g, double b) : this(new ColorRGB(r, g, b)) { }
    public override double Value(double u, double v, Vector3d p, double wavelength)
    {
        return (Albedo.R + Albedo.G + Albedo.B) / 3.0; // Return grayscale value
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

    public CheckerTexture(double scale, ColorRGB odd, ColorRGB even) : this(scale, new SolidColor(odd), new SolidColor(even)) { }

    public override double Value(double u, double v, Vector3d p, double wavelength)
    {
        // Compute the checkerboard pattern
        //double sines = Math.Sin(InvScale * u) * Math.Sin(InvScale * v);
        //return sines < 0 ? Odd.Value(u, v, p) : Even.Value(u, v, p);
        // Texture is actually checkered in space; all that matters is where the hit occurred in 3D space
        int xInteger = (int)Math.Floor(InvScale * p.X);
        int yInteger = (int)Math.Floor(InvScale * p.Y);
        int zInteger = (int)Math.Floor(InvScale * p.Z);
        return (xInteger + yInteger + zInteger) % 2 == 0 ? Odd.Value(u, v, p, wavelength) : Even.Value(u, v, p, wavelength);
    }
}

public class ImageTexture(Image img) : Texture
{

    public ImageTexture(string filename) : this(Image.Load(filename)) { }
    private Image<Rgba32> ImageData = img.CloneAs<Rgba32>();

    public override double Value(double u, double v, Vector3d p, double wavelength)
    {
        // If we have no image, return solid cyan as a debugging aid
        if (ImageData == null)
        {
            if (wavelength >= 490e-9 && wavelength <= 520e-9) return 1.0; // Green
            if (wavelength >= 450e-9 && wavelength < 490e-9) return 0.5; // Blue-green
            if (wavelength >= 620e-9 && wavelength <= 750e-9) return 0.5; // Red
            return 0.0; // Other wavelengths - return black
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
        //return new Color(r, g, b);
        return (r + g + b) / 3.0; // Return grayscale value
    }
}

class NoiseTexture(double scale) : Texture
{
    // Texture with Perlin noise. The larger the value of scale,
    // the higher-frequency the noise.
    private readonly Perlin Noise = new();
    private readonly double Scale = scale;
    public override double Value(double u, double v, Vector3d p, double wavelength)
    {
        //return new Color(1.0, 1.0, 1.0) * 0.5 * (1.0 + Noise.Noise(Scale * p));
        //return new Color(1.0, 1.0, 1.0) * Noise.Turbulence(p, 7);
        return 0.5 * (1.0 + Math.Sin(Scale * p.Z + 10 * Noise.Turbulence(p, 7)));
    }
}