using System.Numerics;
using System.Security.Cryptography;

namespace RayTrace;

public class Camera
{
    // Public
    public double AspectRatio = 1.0; // Width divided by height
    public int ImageWidth = 100; // Rendered image width in pixels
    public int SamplesPerPixel = 10; // Random samples to take for each pixel
    public int MaxDepth = 10; // Maximum number of ray bounces into scene

    // Private
    private double PixelSamplesScale; // Scaling factor for a sum of pixel samples
    private int ImageHeight; // Rendered image height in pixels
    private Vector3d Center; // Camera center location
    private Vector3d Pixel00Loc; // Location of pixel 0,0
    private Vector3d PixelDeltaU; // Offset between pixels (horizontal)
    private Vector3d PixelDeltaV; // Offset between pixels (vertical)
    private Vector3d[,]? ImageData;
    private readonly RTRandom Generator = new();

    public Camera()
    {
        // Do nothing - all handled in Initialize
    }

    public Camera(int imageWidth, double aspectRatio)
    {
        ImageWidth = imageWidth;
        AspectRatio = aspectRatio;
    }

    public void Render(Hittable world)
    {
        Initialize();
        ImageData = new Vector3d[ImageHeight, this.ImageWidth];
        for (int j = 0; j < ImageHeight; j++)
        {
            Console.WriteLine($"\rScanlines remaining: {ImageHeight - j}");
            for (int i = 0; i < ImageWidth; i++)
            {
                Vector3d pixelCenter = Pixel00Loc + (i * PixelDeltaU) + (j * PixelDeltaV);
                Vector3d rayDirection = pixelCenter - Center;
                //Ray r = new(Center, rayDirection);
                Vector3d pixelColor = new(0.0, 0.0, 0.0);
                for (int sample = 0; sample < SamplesPerPixel; sample++)
                {
                    Ray r = GetRay(i, j);
                    pixelColor += RayColor(r, MaxDepth, world);
                }
                ImageData[j, i] = PixelSamplesScale * pixelColor;
            }
        }
    }

    public static double LinearToGamma(double linearComponent)
    {
        // Apply gamma correction to the written image
        if (linearComponent > 0.0)
        {
            return Math.Sqrt(linearComponent);
        }
        return 0.0;
    }

    public void WriteToFile(string outFile = "image.ppm")
    {
        if (ImageData is null)
        {
            Console.WriteLine("No image data to write to file.");
            return;
        }
        Interval intensity = new(0.000, 0.999);
        using (StreamWriter writeText = new StreamWriter(outFile))
        {
            writeText.WriteLine($"P3\n{ImageWidth} {ImageHeight}\n255\n");
            for (int j = 0; j < ImageHeight; j++)
            {
                for (int i = 0; i < ImageWidth; i++)
                {
                    var rgb = ImageData[j, i];

                    double r = LinearToGamma(rgb.X);
                    double g = LinearToGamma(rgb.Y);
                    double b = LinearToGamma(rgb.Z);

                    int ir = (int)(256 * intensity.Clamp(r));
                    int ig = (int)(256 * intensity.Clamp(g));
                    int ib = (int)(256 * intensity.Clamp(b));

                    // Write out the actual data
                    writeText.WriteLine($"{ir} {ig} {ib}\n");
                }
            }
        }
    }

    public void Initialize()
    {
        ImageHeight = (int)((double)ImageWidth / AspectRatio);
        ImageHeight = (ImageHeight < 1) ? 1 : ImageHeight;

        Center = new Vector3d(0.0, 0.0, 0.0);

        // Set up the camera
        double focalLength = 1.0;
        double viewportHeight = 2.0;
        double viewportWidth = viewportHeight * (double)ImageWidth / (double)ImageHeight;
        Center = new(0.0f, 0.0f, 0.0f);

        // Calculate vectors across horizontal and down the vertical viewport edges
        Vector3d viewportU = new(viewportWidth, 0.0f, 0.0f);
        Vector3d viewportV = new(0.0f, (-viewportHeight), 0.0f);

        // Calculate the horizontal and vertical delta vectors from pixel to pixel
        PixelDeltaU = viewportU / ImageWidth;
        PixelDeltaV = viewportV / ImageHeight;

        // Calculate the location of the upper left pixel
        Vector3d viewportUpperLeft = Center - new Vector3d(0.0, 0.0, focalLength) - viewportU / 2.0 - viewportV / 2.0;
        Pixel00Loc = viewportUpperLeft + 0.5 * (PixelDeltaU + PixelDeltaV);

        // For antialiasing
        PixelSamplesScale = 1.0 / SamplesPerPixel;

        // Set up the array to store the pixel colors
        ImageData = new Vector3d[ImageHeight, ImageWidth];
    }

    public Vector3d RayColor(Ray r, int depth, Hittable world)
    {
        // If we've exceeded the bounce limit, no light gathered
        if (depth <= 0)
        {
            return new Vector3d(0.0, 0.0, 0.0);
        }
        // Check if the ray collides with anything
        // Lower limit of 0.001 prevents floating-point nonsense where an intersection can be
        // found immediately after a bounce
        HitRecord rec = new();
        if (world.Hit(r, new Interval(0.001, double.PositiveInfinity), rec))
        {
            // Show the Normal
            //return 0.5 * (rec.Normal + new Vector3d(1.0, 1.0, 1.0));
            // Random reflection
            //Vector3d direction = Generator.RandomVectorOnHemisphere(rec.Normal);
            // Lambertian reflection
            //Vector3d direction = rec.Normal + Generator.RandomUnitVector();
            //return 0.5 * RayColor(new Ray(rec.P, direction), depth - 1, world);
            // Scattering from different materials
            Ray scattered = new(new Vector3d(0.0,0.0,0.0), new Vector3d(1.0,0.0,0.0));
            Vector3d attenuation = new(0.0,0.0,0.0);
            if (rec.Mat.Scatter(r, rec, attenuation, scattered, Generator))
            {
                return attenuation * RayColor(scattered, depth - 1, world);
            }
            // No light returned
            return new(0.0, 0.0, 0.0);
        }
        // Didn't hit anything - return the "sky"
        Vector3d unitDirection = r.Direction.UnitVector;
        double a = 0.5 * (unitDirection.Y + 1.0);
        return (1.0 - a) * new Vector3d(1.0, 1.0, 1.0) + a * new Vector3d(0.5, 0.7, 1.0);
    }

    private Ray GetRay(int i, int j)
    {
        var offset = SampleSquare();
        var pixelSample = Pixel00Loc + ((i + offset.X) * PixelDeltaU) + ((j + offset.Y) * PixelDeltaV);
        var rayOrigin = Center;
        var rayDirection = pixelSample - rayOrigin;
        return new Ray(rayOrigin, rayDirection);
    }

    private Vector3d SampleSquare()
    {
        // Returns the vector to a random point in the [-0.5,-0.5] to [+0.5,+0.5] unit square
        return new Vector3d(Generator.RandomDouble() - 0.5f, Generator.RandomDouble() - 0.5, 0.0);
    }
}