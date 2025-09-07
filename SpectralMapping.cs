using System.Security.Cryptography.X509Certificates;

namespace RayTrace;

public class SpectralMapping
{
    protected float[] Scale;
    protected float[] Data; // [nCoeffs, resolution^3 * 3]
    protected int Resolution;

    // Spectral to sRGB mapping
    public SpectralMapping(string filename)
    {
        // Load the coefficients for the Jakob and Hanika 2019 model from a file
        try
        {
            using BinaryReader reader = new(File.Open(filename, FileMode.Open));
            string init = new string(reader.ReadChars(4));
            if (init != "SPEC")
            {
                Console.WriteLine($"Error: File {filename} does not appear to be a valid binary coefficient file.");
                throw new Exception("Invalid file format");
            }
            Console.WriteLine($"Loading Jakob and Hanika 2019 coefficients from {filename}");
            Resolution = (int)reader.ReadUInt32();
            Scale = new float[Resolution];
            int nCoeffs = 3; // We always use 3 coefficients
            Data = new float[nCoeffs * Resolution * Resolution * Resolution * 3];
            for (int i = 0; i < Resolution; i++)
            {
                Scale[i] = reader.ReadSingle();
            }
            for (int index = 0; index < nCoeffs * Resolution * Resolution * Resolution * 3; index++)
            {
                Data[index] = reader.ReadSingle();
            }
            reader.Close();
            Console.WriteLine($"Loaded {Resolution * Resolution * Resolution * 3} sets of {nCoeffs} coefficients at resolution {Resolution} from {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file {filename}: {ex.Message}");
            throw;
        }
    }

    public (double, double, double) ConvertRGBToJH2019(double r, double g, double b)
    {
        // Convert RGB to Jakob and Hanika 2019 coefficients
        // Given the scale and data arrays loaded from file, set the coefficients for this color
        double[] rgb = [r, g, b];
        for (int j = 0; j < 3; j++)
        {
            rgb[j] = Math.Clamp(rgb[j], 0.0, 1.0);  // Clamp to [0, 1]
        }
        int i = 0;
        for (int j = 1; j < 3; j++)
        {
            if (rgb[j] >= rgb[i]) i = j;
        }

        double z = rgb[i];
        double scale = (Resolution - 1) / z;
        double x = rgb[(i + 1) % 3] * scale;
        double y = rgb[(i + 2) % 3] * scale;

        // Trilinear interpolation
        int xi = Math.Min((int)Math.Floor(x), Resolution - 2);
        int yi = Math.Min((int)Math.Floor(y), Resolution - 2);
        int zi = FindInterval(z);

        int offset = (((i*Resolution+zi)*Resolution+yi)*Resolution+xi)*3;

        double dx = 3;
        double dy = 3 * Resolution;
        double dz = 3 * Resolution * Resolution;

        double x1 = x - xi;
        double y1 = y - yi;
        double z1 = (z - Scale[zi]) / (Scale[zi + 1] - Scale[zi]);
        double x0 = 1.0 - x1;
        double y0 = 1.0 - y1;
        double z0 = 1.0 - z1;

        double[] cVec = new double[3];
        for (int j = 0; j < 3; j++)
        {
            cVec[j] =  Data[j + offset] * x0 * y0 * z0 +
                       Data[j + offset + (int)dx] * x1 * y0 * z0 +
                       Data[j + offset + (int)dy] * x0 * y1 * z0 +
                       Data[j + offset + (int)(dx + dy)] * x1 * y1 * z0 +
                       Data[j + offset + (int)dz] * x0 * y0 * z1 +
                       Data[j + offset + (int)(dx + dz)] * x1 * y0 * z1 +
                       Data[j + offset + (int)(dy + dz)] * x0 * y1 * z1 +
                       Data[j + offset + (int)(dx + dy + dz)] * x1 * y1 * z1;
        }

        return (cVec[0], cVec[1], cVec[2]);
    }

    private int FindInterval(double z)
    {
        // Binary search to find the interval for z
        int left = 0;
        int lastInterval = Resolution - 2;
        int size = lastInterval;
        while (size > 0)
        {
            int half = size >> 1;
            int mid = left + half + 1;
            if (Scale[mid] <= z)
            {
                left = mid;
                size -= half + 1;
            }
            else
            {
                size = half;
            }
        }
        return Math.Min(left, lastInterval);
    }
}