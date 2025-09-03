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
    private Vector3d[] RandomVec = new Vector3d[PointCount];

    public Perlin()
    {
        Generator = new RTRandom();
        for (int i = 0; i < PointCount; i++)
        {
            //RandomValue[i] = Generator.RandomDouble();
            RandomVec[i] = Generator.RandomVector(-1, 1);
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
        Vector3d[,,] c = new Vector3d[2, 2, 2];

        for (int di = 0; di < 2; di++)
        {
            for (int dj = 0; dj < 2; dj++)
            {
                for (int dk = 0; dk < 2; dk++)
                {
                    c[di, dj, dk] = RandomVec[
                        PermX[(i + di) & 255] ^
                        PermY[(j + dj) & 255] ^
                        PermZ[(k + dk) & 255]];
                }
            }
        }
        return PerlinInterp(c, u, v, w);
    }

    private static double PerlinInterp(Vector3d[,,] c, double u, double v, double w)
    {
        // Smoothing with trilinear interpolation
        double uu = u * u * (3 - 2 * u);
        double vv = v * v * (3 - 2 * v);
        double ww = w * w * (3 - 2 * w);
        double accum = 0.0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    Vector3d weightV = new(u - i, v - j, w - k);
                    accum += (i * uu + (1 - i) * (1 - uu))
                           * (j * vv + (1 - j) * (1 - vv))
                           * (k * ww + (1 - k) * (1 - ww))
                           * Vector3d.Dot(c[i, j, k], weightV);
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

    public double Turbulence(Vector3d p, int depth)
    {
        double accum = 0.0;
        Vector3d tempP = p;
        double weight = 1.0;

        for (int i = 0; i < depth; i++)
        {
            accum += weight * Noise(tempP);
            weight *= 0.5;
            tempP *= 2;
        }

        return Math.Abs(accum);
    }
}