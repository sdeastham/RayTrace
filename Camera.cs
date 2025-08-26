using System.Numerics;

namespace RayTrace;

public class Camera
{
    public double AspectRatio = 1.0; // Width divided by height
    public int ImageWidth = 100; // Rendered image width in pixels
    private int ImageHeight; // Rendered image height in pixels
    private Vector3 Center; // Camera center location
    private Vector3 Pixel00Loc; // Location of pixel 0,0
    private Vector3 PixelDeltaU; // Offset between pixels (horizontal)
    private Vector3 PixelDeltaV; // Offset between pixels (vertical)
    private Vector3[,]? ImageData;

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
        ImageData = new Vector3[ImageHeight, this.ImageWidth];
        for (int j = 0; j < ImageHeight; j++)
        {
            Console.WriteLine($"\rScanlines remaining: {ImageHeight - j}");
            for (int i = 0; i < ImageWidth; i++)
            {
                Vector3 pixelCenter = Pixel00Loc + (i * PixelDeltaU) + (j * PixelDeltaV);
                Vector3 rayDirection = pixelCenter - Center;
                Ray r = new(Center, rayDirection);
                Vector3 pixelColor = Camera.RayColor(r, world);
                ImageData[j, i] = pixelColor;
            }
        }
    }

    public void WriteToFile(string outFile="image.ppm")
    {
        if (ImageData is null)
        {
            Console.WriteLine("No image data to write to file.");
            return;
        }
        using (StreamWriter writeText = new StreamWriter(outFile))
        {
            writeText.WriteLine($"P3\n{ImageWidth} {ImageHeight}\n255\n");
            for (int j = 0; j < ImageHeight; j++)
            {
                for (int i = 0; i < ImageWidth; i++)
                {
                    var rgb = ImageData[j, i];

                    int ir = (int)(255.999 * rgb.X);
                    int ig = (int)(255.999 * rgb.Y);
                    int ib = (int)(255.999 * rgb.Z);

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

        Center = new Vector3(0.0f, 0.0f, 0.0f);

        // Set up the camera
        float focalLength = 1.0f;
        float viewportHeight = 2.0f;
        float viewportWidth = viewportHeight * (float)ImageWidth / (float)ImageHeight;
        Center = new(0.0f, 0.0f, 0.0f);

        // Calculate vectors across horizontal and down the vertical viewport edges
        Vector3 viewportU = new(viewportWidth, 0.0f, 0.0f);
        Vector3 viewportV = new(0.0f, -viewportHeight, 0.0f);

        // Calculate the horizontal and vertical delta vectors from pixel to pixel
        PixelDeltaU = viewportU / ImageWidth;
        PixelDeltaV = viewportV / ImageHeight;

        // Calculate the location of the upper left pixel
        Vector3 viewportUpperLeft = Center - new Vector3(0.0f, 0.0f, focalLength) - viewportU / 2.0f - viewportV / 2.0f;
        Pixel00Loc = viewportUpperLeft + 0.5f * (PixelDeltaU + PixelDeltaV);

        // Set up the array to store the pixel colors
        ImageData = new Vector3[ImageHeight, ImageWidth];
    }

    public static Vector3 RayColor(Ray r, Hittable world)
    {
        // Check if the ray collides with anything
        HitRecord rec = new();
        if (world.Hit(r, new Interval(0.0f, float.PositiveInfinity), rec))
        {
            return 0.5f * (rec.Normal + new Vector3(1.0f, 1.0f, 1.0f));
        }
        // Didn't hit anything - return the "sky"
        Vector3 unitDirection = RTVector.UnitVector(r.Direction);
        float a = 0.5f * (unitDirection.Y + 1.0f);
        return (1.0f - a) * new Vector3(1.0f, 1.0f, 1.0f) + a * new Vector3(0.5f, 0.7f, 1.0f);
    }
}