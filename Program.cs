/*
Ray tracing renderer

Implements the v4.0.2 ray tracer of Shirley et al. (2025).
https://raytracing.github.io/books/RayTracingInOneWeekend.html
Retrieved: 2025-08-28
*/

using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace RayTrace;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Set up the world
        HittableList world = new();

        // Set up the materials
        /*
        Material materialGround = new Lambertian(new Vector3d(0.8, 0.8, 0.0));
        Material materialCenter = new Lambertian(new Vector3d(0.1, 0.2, 0.5));
        Material materialLeft = new Dielectric(1.5);
        Material materialBubble = new Dielectric(1.0 / 1.5);
        Material materialRight = new Metal(new Vector3d(0.8, 0.6, 0.2), 1.0);

        // "Ground"
        world.Add(new Sphere(new Vector3d(0.0, -100.5, -1.0), 100.0, materialGround));
        // Sphere directly in front of the camera
        world.Add(new Sphere(new Vector3d (0.0, 0.0, -1.2), 0.5, materialCenter));
        world.Add(new Sphere(new Vector3d(-1.0, 0.0, -1.0), 0.5, materialLeft));
        world.Add(new Sphere(new Vector3d(+1.0, 0.0, -1.0), 0.5, materialRight));
        world.Add(new Sphere(new Vector3d(-1.0, 0.0, -1.0), 0.4, materialBubble));
        */

        /*
        double R = Math.Cos(Math.PI / 4.0);
        Material materialLeft  = new Lambertian(new Vector3d(0.0, 0.0, 1.0));
        Material materialRight = new Lambertian(new Vector3d(1.0, 0.0, 0.0));

        world.Add(new Sphere(new Vector3d(-R, 0, -1.0), R, materialLeft));
        world.Add(new Sphere(new Vector3d( R, 0, -1.0), R, materialRight));
        */
        Texture checker = new CheckerTexture(0.32, new Color(0.2, 0.3, 0.1), new Color(0.9, 0.9, 0.9));
        Material groundMaterial = new Lambertian(checker);
        world.Add(new Sphere(new Vector3d(0, -1000.0, 0), 1000.0, groundMaterial, "Ground"));

        Random sphereGen = new();
        string objectName;

        #if !SIMPLETEST
        for (int a = -11; a < 11; a++)
        {
            for (int b = -11; b < 11; b++)
            {
                double chooseMat = sphereGen.NextDouble();
                Vector3d center = new(a + 0.9 * sphereGen.NextDouble(), 0.2, b + 0.9 * sphereGen.NextDouble());
                Vector3d center2 = center;
                if ((center - new Vector3d(4, 0.2, 0)).Length > 0.9)
                {
                    Material sphereMaterial;
                    if (chooseMat < 0.8)
                    {
                        // Diffuse sphere
                        var albedo = new Color(sphereGen.NextDouble() * sphereGen.NextDouble(),
                                               sphereGen.NextDouble() * sphereGen.NextDouble(),
                                               sphereGen.NextDouble() * sphereGen.NextDouble());
                        sphereMaterial = new Lambertian(albedo);
                        center2 = center + new Vector3d(0, sphereGen.NextDouble() * 0.5, 0);
                        objectName = "SmallDiffuseSphere";
                    }
                    else if (chooseMat < 0.95)
                    {
                        // Metal
                        // Diffuse sphere
                        var albedo = new Vector3d(sphereGen.NextDouble() * 0.5 + 0.5,
                                                    sphereGen.NextDouble() * 0.5 + 0.5,
                                                    sphereGen.NextDouble() * 0.5 + 0.5);
                        var fuzz = sphereGen.NextDouble() * 0.5;
                        sphereMaterial = new Metal(albedo, fuzz);
                        objectName = "SmallMetalSphere";
                    }
                    else
                    {
                        // Glass
                        sphereMaterial = new Dielectric(1.5);
                        objectName = "SmallGlassSphere";
                    }
                    world.Add(new Sphere(center, center2, 0.2, sphereMaterial, objectName));
                }
            }
        }
        #endif

        Material mat1 = new Dielectric(1.5);
        world.Add(new Sphere(new Vector3d(0, 1, 0), 1.0, mat1, "BigGlassSphere"));

        Material mat2 = new Lambertian(new Color(0.4, 0.2, 0.1));
        world.Add(new Sphere(new Vector3d(-4, 1, 0), 1.0, mat2, "BigDiffuseSphere"));

        Material mat3 = new Metal(new Color(0.7, 0.6, 0.5), 0.0);
        world.Add(new Sphere(new Vector3d(4, 1, 0), 1.0, mat3, "BigMetalSphere"));

        Console.WriteLine($"Total object count: {world.Objects.Count}");

        // Use a bounding volume hierarchy (BVH) rather than testing every object for every ray
        world = new HittableList(new BVHNode(world));

        Camera cam = new()
        {
            ImageWidth = 400,
            AspectRatio = 16.0 / 9.0,
            SamplesPerPixel = 10,
            MaxDepth = 50,
            VerticalFOV = 20.0,
            LookAt = new Vector3d(0, 0, 0),
            LookFrom = new Vector3d(13.0, 2.0, 3.0),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.6,
            FocusDist = 10.0,
        };

        // Test render
        bool testRender = false;
        if (testRender)
        {
            // Fire just one ray, into the center of the viewfield
            cam.SamplesPerPixel = 1;
            cam.TestRay(world, cam.ImageWidth / 2, (int)((double)cam.ImageWidth / cam.AspectRatio) / 2);
        }
        else
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            cam.Render(world);
            stopwatch.Stop();
            Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds * 0.001} s");
            cam.WriteToPNG("image.png");
        }
    }
}