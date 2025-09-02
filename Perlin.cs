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
        int i = (int)(4 * p.X) & 255;
        int j = (int)(4 * p.Y) & 255;
        int k = (int)(4 * p.Z) & 255;
        return RandomValue[PermX[i] ^ PermY[j] ^ PermZ[k]];
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