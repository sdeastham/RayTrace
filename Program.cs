using System.Net.NetworkInformation;
using System.Numerics;

namespace RayTrace;

internal class Program
{
    private static void Main(string[] args)
    {
        int imgWidth = 400;
        double aspectRatio = 16.0 / 9.0; // 16:9 image
        
        // Set the image height based on the aspect ratio
        int imgHeight = (int)(imgWidth / aspectRatio);
        imgHeight = (imgHeight < 1) ? 1 : imgHeight;

        // Set up the world
        HittableList world = new();
        // Sphere directly in front of the camera
        world.Add(new Sphere(new Vector3(0.0f, 0.0f, -1.0f), 0.5f));
        // "Ground"
        world.Add(new Sphere(new Vector3(0.0f, -100.5f, -1.0f), 100.0f));

        // Set up the camera
        float focalLength = 1.0f;
        float viewportHeight = 2.0f;
        float viewportWidth = viewportHeight * (float)imgWidth / (float)imgHeight;
        Vector3 cameraCenter = new(0.0f, 0.0f, 0.0f);

        // Calculate vectors across horizontal and down the vertical viewport edges
        Vector3 viewportU = new(viewportWidth, 0.0f, 0.0f);
        Vector3 viewportV = new(0.0f, -viewportHeight, 0.0f);

        // Calculate the horizontal and vertical delta vectors from pixel to pixel
        Vector3 pixelDeltaU = viewportU / imgWidth;
        Vector3 pixelDeltaV = viewportV / imgHeight;

        // Calculate the location of the upper left pixel
        Vector3 viewportUpperLeft = cameraCenter - new Vector3(0.0f, 0.0f, focalLength) - viewportU / 2.0f - viewportV / 2.0f;
        Vector3 pixel00Loc = viewportUpperLeft + 0.5f * (pixelDeltaU + pixelDeltaV);

        // Simple test image
        //Vector3[,] imgData = TestImg(imgHeight, imgWidth);

        Vector3[,] imgData = new Vector3[imgHeight, imgWidth];
        for (int j = 0; j < imgHeight; j++)
        {
            Console.WriteLine($"\rScanlines remaining: {imgHeight - j}");
            for (int i = 0; i < imgWidth; i++)
            {
                Vector3 pixelCenter = pixel00Loc + (i * pixelDeltaU) + (j * pixelDeltaV);
                Vector3 rayDirection = pixelCenter - cameraCenter;
                Ray r = new(cameraCenter, rayDirection);
                Vector3 pixelColor = RayColor(r,world);
                imgData[j, i] = pixelColor;
            }
        }

        ImgToFile("image.ppm", imgData);
    }

    public static float HitSphere(Vector3 center, float radius, Ray r)
    {
        Vector3 oc = center - r.Origin;
        float a = r.Direction.LengthSquared();
        float h = Vector3.Dot(r.Direction, oc);
        float c = oc.LengthSquared() - radius * radius;
        float discriminant = h * h - a * c;
        if (discriminant < 0.0f)
        {
            return -1.0f;
        }
        return (h - (float)Math.Sqrt(discriminant)) / a;
    }

    public static Vector3 UnitVector(Vector3 v)
    {
        return v / v.Length();
    }

    public static Vector3 RayColor(Ray r, Hittable world)
    {
        // Check if the ray collides with anything
        HitRecord rec = new();
        if (world.Hit(r, 0.0f, float.PositiveInfinity, rec))
        {
            return 0.5f * (rec.Normal + new Vector3(1.0f, 1.0f, 1.0f));
        }
        // Didn't hit anything - return the "sky"
        Vector3 unitDirection = UnitVector(r.Direction);
        float a = 0.5f * (unitDirection.Y + 1.0f);
        return (1.0f - a) * new Vector3(1.0f, 1.0f, 1.0f) + a * new Vector3(0.5f, 0.7f, 1.0f);
    }

    public static Vector3[,] TestImg(int imgHeight, int imgWidth)
    {
        Vector3[,] imgData = new Vector3[imgHeight, imgWidth];
        for (int j = 0; j < imgHeight; j++)
        {
            Console.WriteLine($"\rScanlines remaining: {imgHeight - j}");
            for (int i = 0; i < imgWidth; i++)
            {
                float r = (float)((double)i / (double)(imgWidth - 1));
                float g = (float)((double)j / (double)(imgHeight - 1));
                float b = 0.0f;

                imgData[j, i] = new Vector3(r, g, b);
            }
        }
        return imgData;
    }
    public static void ImgToFile(string imagePath, Vector3[,] imgData)
    {
        int imageWidth = imgData.GetLength(1);
        int imageHeight = imgData.GetLength(0);

        using (StreamWriter writeText = new StreamWriter("image.ppm"))
        {
            writeText.WriteLine($"P3\n{imageWidth} {imageHeight}\n255\n");
            for (int j = 0; j < imageHeight; j++)
            {
                for (int i = 0; i < imageWidth; i++)
                {
                    var rgb = imgData[j, i];

                    int ir = (int)(255.999 * rgb.X);
                    int ig = (int)(255.999 * rgb.Y);
                    int ib = (int)(255.999 * rgb.Z);

                    // Write out the actual data
                    writeText.WriteLine($"{ir} {ig} {ib}\n");
                }
            }
        }
    }
}