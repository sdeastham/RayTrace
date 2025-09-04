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
        switch (9)
        {
            case 1: await BouncingSpheres(); break;
            case 2: await CheckeredSpheres(); break;
            case 3: await Earth(); break;
            case 4: await PerlinSpheres(); break;
            case 5: await Quads(); break;
            case 6: await SimpleLight(); break;
            case 7: await CornellBox(); break;
            case 8: await CornellSmoke(); break;
            case 9: await FinalScene(400, 20, 4); break;
            default: await FinalScene(400, 250, 4); break;
        }
    }

    private static async Task FinalScene(int imageWidth, int samplesPerPixel, int maxDepth)
    {
        HittableList boxes1 = new();
        Material ground = new Lambertian(new Color(0.48, 0.83, 0.53));
        RTRandom generator = new();

        int boxesPerSide = 20;
        for (int i = 0; i < boxesPerSide; i++)
        {
            for (int j = 0; j < boxesPerSide; j++)
            {
                double w = 100.0;
                double x0 = -1000.0 + i * w;
                double z0 = -1000.0 + j * w;
                double y0 = 0.0;
                double x1 = x0 + w;
                double y1 = 100.0 * generator.RandomDouble();
                double z1 = z0 + w;
                boxes1.Add(Quad.Box(new Vector3d(x0, y0, z0), new Vector3d(x1, y1, z1), ground));
            }
        }
        HittableList world = new();
        world.Add(new BVHNode(boxes1));

        Material light = new DiffuseLight(new Color(7, 7, 7));
        world.Add(new Quad(new Vector3d(123, 554, 147), new Vector3d(300, 0, 0), new Vector3d(0, 0, 265), light));

        Vector3d center1 = new(400, 400, 200);
        Vector3d center2 = center1 + new Vector3d(30, 0, 0);
        Material movingSphereMaterial = new Lambertian(new Color(0.7, 0.3, 0.1));
        world.Add(new Sphere(center1, center2, 50, movingSphereMaterial, "MovingSphere"));
        world.Add(new Sphere(new Vector3d(260, 150, 45), 50, new Dielectric(1.5), "InnerGlassSphere"));
        world.Add(new Sphere(new Vector3d(0, 150, 145), 50, new Metal(new Color(0.8, 0.8, 0.9), 1.0), "MetalSphere"));

        Hittable boundary = new Sphere(new Vector3d(260, 150, 145), 50, new Dielectric(1.5), "GlassSphereHittable");
        world.Add(boundary);
        world.Add(new ConstantMedium(boundary, 0.2, new Color(0.2, 0.4, 0.9), "InnerConstantMedium"));
        boundary = new Sphere(new Vector3d(0, 0, 0), 5000, new Dielectric(1.5), "WorldGasHittable");
        world.Add(new ConstantMedium(boundary, 0.0001, new Color(1, 1, 1), "WorldConstantMedium"));

        ITexture earthTexture = new ImageTexture("earthmap.jpg");
        world.Add(new Sphere(new Vector3d(400, 200, 400), 100, new Lambertian(earthTexture), "Earth"));

        ITexture perlinTexture = new NoiseTexture(0.2);
        world.Add(new Sphere(new Vector3d(220, 280, 300), 80, new Lambertian(perlinTexture), "PerlinSphere"));

        HittableList boxes2 = new();
        Material white = new Lambertian(new Color(0.73, 0.73, 0.73));
        int ns = 1000;
        for (int j = 0; j < ns; j++)
        {
            boxes2.Add(new Sphere(new Vector3d(165 * generator.RandomDouble(), 165 * generator.RandomDouble(), 165 * generator.RandomDouble()), 10, white, "SmallSphere"));
        }

        world.Add(new Translate(new RotateY(new BVHNode(boxes2), 15), new Vector3d(-100, 270, 395)));

        Camera cam = new()
        {
            ImageWidth = imageWidth,
            AspectRatio = 1.0,
            SamplesPerPixel = samplesPerPixel,
            MaxDepth = maxDepth,
            VerticalFOV = 40.0,
            LookAt = new Vector3d(278, 278, 0),
            LookFrom = new Vector3d(478, 278, -600),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.0,
            Background = new Color(0, 0, 0),
        };

        cam.Render(world);
        cam.WriteToPNG("finalscene.png");
    }

    private static async Task CornellSmoke()
    {
        HittableList world = new();

        Material red = new Lambertian(new Color(0.65, 0.05, 0.05));
        Material white = new Lambertian(new Color(0.73, 0.73, 0.73));
        Material green = new Lambertian(new Color(0.12, 0.45, 0.15));
        Material light = new DiffuseLight(new Color(7, 7, 7));

        world.Add(new Quad(new Vector3d(555, 0, 0), new Vector3d(0, 0, 555), new Vector3d(0, 555, 0), green, "Left"));
        world.Add(new Quad(new Vector3d(0, 0, 0), new Vector3d(0, 555, 0), new Vector3d(0, 0, 555), red, "Right"));
        world.Add(new Quad(new Vector3d(113, 554, 127), new Vector3d(330, 0, 0), new Vector3d(0, 0, 305), light, "Light"));
        world.Add(new Quad(new Vector3d(0, 555, 0), new Vector3d(555, 0, 0), new Vector3d(0, 0, 555), white, "Lower"));
        world.Add(new Quad(new Vector3d(0, 0, 0), new Vector3d(555, 0, 0), new Vector3d(0, 0, 555), white, "Upper"));
        world.Add(new Quad(new Vector3d(0, 0, 555), new Vector3d(555, 0, 0), new Vector3d(0, 555, 0), white, "Back"));

        Hittable box1 = Quad.Box(new Vector3d(0, 0, 0), new Vector3d(165, 330, 165), white);
        box1 = new RotateY(box1, 15);
        box1 = new Translate(box1, new Vector3d(265, 0, 295));

        Hittable box2 = Quad.Box(new Vector3d(0, 0, 0), new Vector3d(165, 165, 165), white);
        box2 = new RotateY(box2, -18);
        box2 = new Translate(box2, new Vector3d(130, 0, 65));

        world.Add(new ConstantMedium(box1, 0.01, new Color(0, 0, 0), "Fog"));
        world.Add(new ConstantMedium(box2, 0.01, new Color(1, 1, 1), "Smoke"));

        Camera cam = new()
        {
            ImageWidth = 600,
            AspectRatio = 1.0,
            SamplesPerPixel = 200,
            MaxDepth = 50,
            VerticalFOV = 40.0,
            LookAt = new Vector3d(278, 278, 0),
            LookFrom = new Vector3d(278, 278, -800),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.0,
            Background = new Color(0, 0, 0),
        };
        cam.Render(world);
        cam.WriteToPNG("cornellsmoke.png");
    }

    private static async Task CornellBox()
    {
        HittableList world = new();

        Material red = new Lambertian(new Color(0.65, 0.05, 0.05));
        Material white = new Lambertian(new Color(0.73, 0.73, 0.73));
        Material green = new Lambertian(new Color(0.12, 0.45, 0.15));
        Material light = new DiffuseLight(new Color(15, 15, 15));

        world.Add(new Quad(new Vector3d(555, 0, 0), new Vector3d(0, 0, 555), new Vector3d(0, 555, 0), green, "Left"));
        world.Add(new Quad(new Vector3d(0, 0, 0), new Vector3d(0, 555, 0), new Vector3d(0, 0, 555), red, "Right"));
        world.Add(new Quad(new Vector3d(343, 554, 332), new Vector3d(-130, 0, 0), new Vector3d(0, 0, -105), light, "Light"));
        world.Add(new Quad(new Vector3d(0, 0, 0), new Vector3d(555, 0, 0), new Vector3d(0, 0, 555), white, "Lower"));
        world.Add(new Quad(new Vector3d(555, 555, 555), new Vector3d(-555, 0, 0), new Vector3d(0, 0, -555), white, "Upper"));
        world.Add(new Quad(new Vector3d(0, 0, 555), new Vector3d(555, 0, 0), new Vector3d(0, 555, 0), white, "Back"));

        Hittable box1 = Quad.Box(new Vector3d(0, 0, 0), new Vector3d(165, 330, 165), white);
        box1 = new RotateY(box1, 15);
        box1 = new Translate(box1, new Vector3d(265, 0, 295));
        world.Add(box1);

        Hittable box2 = Quad.Box(new Vector3d(0, 0, 0), new Vector3d(165, 165, 165), white);
        box2 = new RotateY(box2, -18);
        box2 = new Translate(box2, new Vector3d(130, 0, 65));
        world.Add(box2);

        Camera cam = new()
        {
            ImageWidth = 600,
            AspectRatio = 1.0,
            SamplesPerPixel = 10,
            MaxDepth = 50,
            VerticalFOV = 40.0,
            LookAt = new Vector3d(278, 278, 0),
            LookFrom = new Vector3d(278, 278, -800),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.0,
            Background = new Color(0, 0, 0),
        };
        cam.Render(world);
        cam.WriteToPNG("cornellbox.png");
    }

    private static async Task SimpleLight()
    {
        HittableList world = new();
        Texture perlinTexture = new NoiseTexture(4.0);
        world.Add(new Sphere(new Vector3d(0, -1000, 0), 1000, new Lambertian(perlinTexture), "Ground"));
        world.Add(new Sphere(new Vector3d(0, 2, 0), 2, new Lambertian(perlinTexture), "Sphere"));

        Material diffLight = new DiffuseLight(new Color(4, 4, 4));
        world.Add(new Quad(new Vector3d(3, 1, -2), new Vector3d(2, 0, 0), new Vector3d(0, 2, 0), diffLight, "LightQuad"));
        world.Add(new Sphere(new Vector3d(0, 7, 0), 2, diffLight, "LightSphere"));

        Camera cam = new()
        {
            ImageWidth = 400,
            AspectRatio = 16.0 / 9.0,
            SamplesPerPixel = 50,
            MaxDepth = 50,
            Background = new Color(0, 0, 0),

            VerticalFOV = 20.0,
            LookAt = new Vector3d(0, 2, 0),
            LookFrom = new Vector3d(26, 3, 6),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.0,
        };
        cam.Render(world);
        cam.WriteToPNG("light.png");
    }

    private static async Task Quads()
    {
        HittableList world = new();
        // Materials
        Material leftRed = new Lambertian(new Color(1.0, 0.2, 0.2));
        Material backGreen = new Lambertian(new Color(0.2, 1.0, 0.2));
        Material rightBlue = new Lambertian(new Color(0.2, 0.2, 1.0));
        Material upperOrange = new Lambertian(new Color(1.0, 0.5, 0.0));
        Material lowerTeal = new Lambertian(new Color(0.2, 0.8, 0.8));

        // Quads
        world.Add(new Quad(new Vector3d(-3, -2, 5), new Vector3d(0, 0, -4), new Vector3d(0, 4, 0), leftRed, "Left"));
        world.Add(new Quad(new Vector3d(-2, -2, 0), new Vector3d(4, 0, 0), new Vector3d(0, 4, 0), backGreen, "Back"));
        world.Add(new Quad(new Vector3d(3, -2, 1), new Vector3d(0, 0, 4), new Vector3d(0, 4, 0), rightBlue, "Right"));
        world.Add(new Quad(new Vector3d(-2, 3, 1), new Vector3d(4, 0, 0), new Vector3d(0, 0, 4), upperOrange, "Upper"));
        world.Add(new Quad(new Vector3d(-2, -3, 5), new Vector3d(4, 0, 0), new Vector3d(0, 0, -4), lowerTeal, "Lower"));

        // Camera
        Camera cam = new()
        {
            ImageWidth = 400,
            AspectRatio = 1.0,
            SamplesPerPixel = 10,
            MaxDepth = 50,
            VerticalFOV = 80.0,
            LookAt = new Vector3d(0, 0, 0),
            LookFrom = new Vector3d(0.0, 0.0, 9.0),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.0,
            Background = new Color(0.7, 0.8, 1.0),
        };
        cam.Render(world);
        cam.WriteToPNG("quads.png");
    }

    private static async Task PerlinSpheres()
    {
        HittableList world = new();
        ITexture perlinTexture = new NoiseTexture(4.0);
        world.Add(new Sphere(new Vector3d(0, -1000, 0), 1000, new Lambertian(perlinTexture), "Ground"));
        world.Add(new Sphere(new Vector3d(0, 2, 0), 2, new Lambertian(perlinTexture), "Sphere"));
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
            DefocusAngle = 0.0,
            Background = new Color(0.7, 0.8, 1.0),
        };
        cam.Render(world);
        cam.WriteToPNG("perlin.png");
    }

    private static async Task Earth()
    {
        HittableList world = new();
        ITexture earthTexture = new ImageTexture("earthmap.jpg");
        Material earthSurface = new Lambertian(earthTexture);
        world.Add(new Sphere(new Vector3d(0, 0, 0), 2, earthSurface, "Earth"));
        Camera cam = new()
        {
            ImageWidth = 400,
            AspectRatio = 16.0 / 9.0,
            SamplesPerPixel = 10,
            MaxDepth = 50,
            VerticalFOV = 20.0,
            LookAt = new Vector3d(0, 0, 0),
            LookFrom = new Vector3d(0.0, 0.0, 12.0),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.0,
            Background = new Color(0.7, 0.8, 1.0),
        };
        cam.Render(world);
        cam.WriteToPNG("earth.png");
    }

    private static async Task CheckeredSpheres()
    {
        HittableList world = new();
        var checker = new CheckerTexture(0.32, new Color(0.2, 0.3, 0.1), new Color(0.9, 0.9, 0.9));
        world.Add(new Sphere(new Vector3d(0, -10, 0), 10, new Lambertian(checker), "Lower"));
        world.Add(new Sphere(new Vector3d(0, 10, 0), 10, new Lambertian(checker), "Upper"));
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
            DefocusAngle = 0.0,
            Background = new Color(0.7, 0.8, 1.0),
        };
        cam.Render(world);
        cam.WriteToPNG("checkered.png");
    }

    private static async Task BouncingSpheres()
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
            Background = new Color(0.7, 0.8, 1.0),
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
            cam.Render(world);
            cam.WriteToPNG("bouncing.png");
        }
    }
}