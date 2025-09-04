using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RayTrace;

public class Camera
{
    // Public
    public double AspectRatio = 1.0; // Width divided by height
    public int ImageWidth = 100; // Rendered image width in pixels
    public int SamplesPerPixel = 10; // Random samples to take for each pixel
    public int MaxDepth = 10; // Maximum number of ray bounces into scene
    public double VerticalFOV = 90.0; // Vertical view angle (field of view)
    public Vector3d LookFrom = new(0.0, 0.0, 0.0); // Point camera is looking from
    public Vector3d LookAt = new(0.0, 0.0, -1.0); // Point camera is looking at
    public Vector3d UpVector = new(0.0, 1.0, 0.0); // Camera-relative "up" direction
    public double DefocusAngle = 0.0; // Variation angle of rays through each pixel
    public double FocusDist = 10.0; // Distance from camera LookFrom point to the plane of perfect focus
    public Color Background = new(0.0, 0.0, 0.0); // Scene background color

    // Private
    private double PixelSamplesScale; // Scaling factor for a sum of pixel samples
    private int SqrtSamplesPerPixel; // Square root of the number of samples per pixel, for stratified sampling
    private double RecipSqrtSamplesPerPixel; // Reciprocal of the above
    public int ImageHeight { get; private set; } // Rendered image height in pixels
    private Vector3d? Center; // Camera center location
    private Vector3d? Pixel00Loc; // Location of pixel 0,0
    private Vector3d? PixelDeltaU; // Offset between pixels (horizontal)
    private Vector3d? PixelDeltaV; // Offset between pixels (vertical)
    private Vector3d? UBasis, VBasis, WBasis; // Camera frame basis vectors
    private Vector3d? DefocusDiskU; // Defocus disk horizontal radius
    private Vector3d? DefocusDiskV; // Defocus disk vertical radius
    private Vector3d[,]? ImageData;
    private readonly RTRandom Generator = new();
    private bool PrettyPrint = false; 

    public Camera()
    {
        // Do nothing - all handled in Initialize
    }

    public Camera(int imageWidth, double aspectRatio)
    {
        ImageWidth = imageWidth;
        AspectRatio = aspectRatio;
    }

    public Vector3d RenderSingle(int i, int j, Hittable world)
    {
        Vector3d pixelColor = new(0.0, 0.0, 0.0);
        // With stratified sampling
        for (int sJ = 0; sJ < SqrtSamplesPerPixel; sJ++)
        {
            for (int sI = 0; sI < SqrtSamplesPerPixel; sI++)
            {
                Ray r = GetRay(i, j, sI, sJ);
                pixelColor += RayColor(r, MaxDepth, world);
            }
        }
        // Without stratified sampling
        /*
        for (int sample = 0; sample < SamplesPerPixel; sample++)
        {
            Ray r = GetRay(i, j);
            pixelColor += RayColor(r, MaxDepth, world);
        }
        */
        return PixelSamplesScale * pixelColor;
    }

    public void TestRay(Hittable world, int i, int j)
    {
        Initialize();
        //Console.Clear();
        Console.WriteLine($"Testing ray through pixel {i},{j}");
        // i is in the x direction (width), j in the y direction (height)
        // This would normally be stored in ImageData[j,i]
        Vector3d pixelColor = RenderSingle(i, j, world);
        Console.WriteLine($"Test complete. Pixel color: {pixelColor.X:F6}, {pixelColor.Y:F6}, {pixelColor.Z:F6}");
    }

    public void Render(Hittable world)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        Initialize();
        if (ImageData is null)
        {
            Console.WriteLine("ImageData field has not been initialized; aborting.");
            return;
        }
        int totalIterations = ImageHeight * ImageWidth;
        int completedIterations = 0;
        //Console.Clear();
        Console.WriteLine($"Beginning render ({totalIterations} pixels).");
        Parallel.For(0, totalIterations, ij =>
        {
            int i = ij % ImageWidth;
            int j = ij / ImageWidth; // Integer division
            ImageData[j, i] = RenderSingle(i, j, world);
            Interlocked.Increment(ref completedIterations);
            if (completedIterations % 100 == 0)
            {
                if (PrettyPrint)
                {
                    Console.SetCursorPosition(0, 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, 1);
                    Console.Write($"Progress: {completedIterations:D6}/{totalIterations:D6} ({((double)completedIterations / totalIterations) * 100:F1}%)");
                }
                else
                {
                    int progressInt = (int)(10 * completedIterations / totalIterations);
                    Console.Write(progressInt);
                }
            }
        });
        Console.Write("\n");
        stopwatch.Stop();
        Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds * 0.001} s");
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

    public void WriteToPNG(string outfile = "image.png")
    {
        if (ImageData is null)
        {
            Console.WriteLine("No image data to write to file.");
            return;
        }
        Interval intensity = new(0.000, 0.999);

        // Use SixLabors instead of the Bitmap class from System.Drawing
        // This provides better cross-platform compatibility, as Bitmap
        // only works on Windows.
        using var image = new Image<Rgba32>(ImageWidth, ImageHeight);
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
                image[i, j] = new Rgba32((byte)ir, (byte)ig, (byte)ib, 255);
            }
        }
        image.Save(outfile);
    }

    public void WriteToPPM(string outFile = "image.ppm")
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

        Center = LookFrom;

        // Set up the camera and determine viewport dimensions
        //double focalLength = (LookFrom - LookAt).Length;
        double theta = VerticalFOV * Math.PI / 180.0;
        double h = Math.Tan(theta / 2.0);
        double viewportHeight = 2.0 * h * FocusDist;
        double viewportWidth = viewportHeight * (double)ImageWidth / (double)ImageHeight;

        // Calculate the u, v, w unit basis vectors for the camera coordinate frame
        WBasis = (LookFrom - LookAt).UnitVector;
        UBasis = Vector3d.Cross(UpVector, WBasis).UnitVector;
        VBasis = Vector3d.Cross(WBasis, UBasis);

        // Calculate vectors across horizontal and down the vertical viewport edges
        Vector3d viewportU = viewportWidth * UBasis;
        Vector3d viewportV = viewportHeight * -VBasis;

        // Calculate the horizontal and vertical delta vectors from pixel to pixel
        PixelDeltaU = viewportU / ImageWidth;
        PixelDeltaV = viewportV / ImageHeight;

        // Calculate the location of the upper left pixel
        Vector3d viewportUpperLeft = Center - (FocusDist * WBasis) - viewportU / 2.0 - viewportV / 2.0;
        Pixel00Loc = viewportUpperLeft + 0.5 * (PixelDeltaU + PixelDeltaV);

        // Calculate the camera defocus disk basis vectors
        double defocusRadius = FocusDist * Math.Tan((DefocusAngle / 2.0) * Math.PI / 180.0);
        DefocusDiskU = UBasis * defocusRadius;
        DefocusDiskV = VBasis * defocusRadius;

        // For antialiasing
        SqrtSamplesPerPixel = (int)Math.Sqrt(SamplesPerPixel);
        // Need to compensate for the fact that we might 
        // not have a perfect square number of samples
        PixelSamplesScale = 1.0 / (SqrtSamplesPerPixel * SqrtSamplesPerPixel);
        RecipSqrtSamplesPerPixel = 1.0 / SqrtSamplesPerPixel;

        // Set up the array to store the pixel colors
        ImageData = new Vector3d[ImageHeight, ImageWidth];
    }

    public Vector3d RayColor(Ray r, int depth, Hittable world)
    {
        // If we've exceeded the bounce limit, no light gathered
        #if SINGLERAY
        Console.WriteLine($"Ray generated at depth {depth}");
        #endif
        if (depth <= 0)
        {
            return new Vector3d(0.0, 0.0, 0.0);
        }
        // Check if the ray collides with anything
        // Lower limit of 0.001 prevents floating-point nonsense where an intersection can be
        // found immediately after a bounce
        HitRecord rec = new();
        if (!world.Hit(r, new Interval(0.001, double.PositiveInfinity), rec)) return Background;
        Ray scattered = new(new Vector3d(0.0, 0.0, 0.0), new Vector3d(1.0, 0.0, 0.0));
        Color attenuation = new(0.0, 0.0, 0.0);
        Color colorFromEmission = rec.Mat.Emitted(rec.U, rec.V, rec.P);
        if (!rec.Mat.Scatter(r, rec, attenuation, scattered, Generator)) return colorFromEmission;

        double scatteringPDF = rec.Mat.ScatteringPDF(r, rec, scattered);
        double valuePDF = scatteringPDF;
        Color colorFromScatter = attenuation * scatteringPDF * RayColor(scattered, depth - 1, world) / valuePDF;
        return colorFromEmission + colorFromScatter;
    }

    private Ray GetRay(int i, int j, int sI, int sJ)
    {
        // Construct a camera ray originating from the defocus disk and directed at
        // a randomly-sampled point around the pixel location i, j for stratified sample square sI, sJ
        var offset = SampleSquareStratified(sI,sJ);
        var pixelSample = Pixel00Loc + ((i + offset.X) * PixelDeltaU) + ((j + offset.Y) * PixelDeltaV);
        Vector3d rayOrigin = (DefocusAngle <= 0.0) ? Center : DefocusDiskSample();
        var rayDirection = pixelSample - rayOrigin;
        double rayTime = Generator.RandomDouble();
        return new Ray(rayOrigin, rayDirection, rayTime);
    }

    private Vector3d SampleSquare()
    {
        // Returns the vector to a random point in the [-0.5,-0.5] to [+0.5,+0.5] unit square
        return new Vector3d(Generator.RandomDouble() - 0.5f, Generator.RandomDouble() - 0.5, 0.0);
    }

    private Vector3d SampleSquareStratified(int sI, int sJ)
    {
        // Returns the vector to a random point in the [-0.5,-0.5] to [+0.5,+0.5] unit square
        // using stratified sampling within the sqrtSamplesPerPixel x sqrtSamplesPerPixel sub-square
        double jitterX = Generator.RandomDouble();
        double jitterY = Generator.RandomDouble();
        return new Vector3d((sI + jitterX) * RecipSqrtSamplesPerPixel - 0.5,
                            (sJ + jitterY) * RecipSqrtSamplesPerPixel - 0.5,
                            0.0);
    }

    private Vector3d DefocusDiskSample()
    {
        Vector3d p = Generator.RandomVectorInUnitDisk();
        return Center + (p.X * DefocusDiskU) + (p.Y * DefocusDiskV);
    }
}