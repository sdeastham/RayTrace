namespace RayTrace;

public class ColorRGB(double r, double g, double b) : Vector3d(r, g, b)
{
    public static readonly ColorRGB Black = new(0, 0, 0);
    public static readonly ColorRGB White = new(1, 1, 1);
    public double R => Data[0];
    public double G => Data[1];
    public double B => Data[2];

    public static ColorRGB operator *(ColorRGB c, Vector3d v) => new(c.R * v.X, c.G * v.Y, c.B * v.Z);
    public static ColorRGB operator *(ColorRGB c, double d) => new(c.R * d, c.G * d, c.B * d);
    public static ColorRGB operator *(double d, ColorRGB c) => c * d;
    public static ColorRGB operator /(ColorRGB c, double d) => new(c.R / d, c.G / d, c.B / d);
    public static ColorRGB operator +(ColorRGB c1, ColorRGB c2) => new(c1.R + c2.R, c1.G + c2.G, c1.B + c2.B);
    public static ColorRGB operator -(ColorRGB c1, ColorRGB c2) => new(c1.R - c2.R, c1.G - c2.G, c1.B - c2.B);
}

public class ColorXYZ(double x, double y, double z) : Vector3d(x, y, z)
{
    public static readonly ColorXYZ Black = new(0, 0, 0);
    public static readonly ColorXYZ White = new(1, 1, 1);

    public static ColorXYZ operator *(ColorXYZ c, Vector3d v) => new(c.X * v.X, c.Y * v.Y, c.Z * v.Z);
    public static ColorXYZ operator *(ColorXYZ c, double d) => new(c.X * d, c.Y * d, c.Z * d);
    public static ColorXYZ operator *(double d, ColorXYZ c) => c * d;
    public static ColorXYZ operator /(ColorXYZ c, double d) => new(c.X / d, c.Y / d, c.Z / d);
    public static ColorXYZ operator +(ColorXYZ c1, ColorXYZ c2) => new(c1.X + c2.X, c1.Y + c2.Y, c1.Z + c2.Z);
    public static ColorXYZ operator -(ColorXYZ c1, ColorXYZ c2) => new(c1.X - c2.X, c1.Y - c2.Y, c1.Z - c2.Z);

    public ColorRGB ToRGB()
    {
        // Convert from CIE 1931 XYZ to linear sRGB
        // Need to investigate where this came from..
        double r = +3.2406 * X - 1.5372 * Y - 0.4986 * Z;
        double g = -0.9689 * X + 1.8758 * Y + 0.0415 * Z;
        double b = +0.0557 * X - 0.2040 * Y + 1.0570 * Z;
        return new ColorRGB(r, g, b);
    }

    public void Add(double intensity, double wavelength)
    {
        ColorXYZ xyz = FromWavelength(wavelength);
        xyz *= intensity;
        Data[0] += xyz.X;
        Data[1] += xyz.Y;
        Data[2] += xyz.Z;
    }

    public static ColorXYZ FromWavelength(double wavelength)
    {
        // Convert wavelength in nm to CIE 1931 XYZ color space
        // Uses the Wyman 2013 approximation
        // Important: wavelength should be in nm, not m!
        if (wavelength < 380.0 || wavelength > 780.0)
            return ColorXYZ.Black;

        double x, y, z;
        double t1 = (wavelength - 442.0) * ((wavelength < 442.0) ? 0.0624 : 0.0374);
        double t2 = (wavelength - 599.8) * ((wavelength < 599.8) ? 0.0264 : 0.0323);
        double t3 = (wavelength - 501.1) * ((wavelength < 501.1) ? 0.0490 : 0.0382);
        x = 0.362 * Math.Exp(-0.5 * t1 * t1) + 1.056 * Math.Exp(-0.5 * t2 * t2) - 0.065 * Math.Exp(-0.5 * t3 * t3);
        t1 = (wavelength - 568.8) * ((wavelength < 568.8) ? 0.0213 : 0.0247);
        t2 = (wavelength - 530.9) * ((wavelength < 530.9) ? 0.0613 : 0.0322);
        y = 0.821 * Math.Exp(-0.5 * t1 * t1) + 0.286 * Math.Exp(-0.5 * t2 * t2);
        t1 = (wavelength - 437.0) * ((wavelength < 437.0) ? 0.0845 : 0.0278);
        t2 = (wavelength - 459.0) * ((wavelength < 459.0) ? 0.0385 : 0.0725);
        z = 1.217 * Math.Exp(-0.5 * t1 * t1) + 0.681 * Math.Exp(-0.5 * t2 * t2);

        return new ColorXYZ(x, y, z);
    }
}