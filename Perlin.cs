using System.Runtime.CompilerServices;

namespace RayTrace;

public class Perlin
{
    private readonly RTRandom Generator;
    private const int PointCount = 256;
    private readonly double[] RandomValue = new double[PointCount];
    private readonly int[] PermX = new int[PointCount];
    private readonly int[] PermY = new int[PointCount];
    private readonly int[] PermZ = new int[PointCount];

    public Perlin()
    {
        Generator = new RTRandom();
        for (int i = 0; i < PointCount; i++)
        {
            RandomValue[i] = Generator.RandomDouble();
        }
        PerlinGeneratePerm(PermX);
        PerlinGeneratePerm(PermY);
        PerlinGeneratePerm(PermZ);
    }

    public double Noise(Vector3d p)
    {
        double u = p.X - Math.Floor(p.X);
        double v = p.Y - Math.Floor(p.Y);
        double w = p.Z - Math.Floor(p.Z);

        int i = (int)Math.Floor(p.X);
        int j = (int)Math.Floor(p.Y);
        int k = (int)Math.Floor(p.Z);

        double[,,] c = new double[2, 2, 2];

        for (int di = 0; di < 2; di++)
        {
            for (int dj = 0; dj < 2; dj++)
            {
                for (int dk = 0; dk < 2; dk++)
                {
                    c[di, dj, dk] = RandomValue[
                        PermX[(i + di) & 255] ^
                        PermY[(j + dj) & 255] ^
                        PermZ[(k + dk) & 255]];
                }
            }
        }
        return TrilinearInterp(c, u, v, w);
    }

    private static double TrilinearInterp(double[,,] c, double u, double v, double w)
    {
        // Smoothing with trilinear interpolation
        double accum = 0.0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    accum += (i*u + (1-i)*(1-u))
                           * (j*v + (1-j)*(1-v))
                           * (k*w + (1-k)*(1-w))
                           * c[i, j, k];
                }
            }
        }
        return accum;
    }

    private void PerlinGeneratePerm(int[] perm)
    {
        for (int i = 0; i < PointCount; i++) perm[i] = i;
        Permute(perm, PointCount);
    }

    private void Permute(int[] p, int n)
    {
        for (int i = n - 1; i > 0; i--)
        {
            int j = Generator.RandomInt(0, i);
            // Swap!
            (p[i], p[j]) = (p[j], p[i]);
        }
    }
}