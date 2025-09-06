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
using SixLabors.ImageSharp.ColorSpaces;

namespace RayTrace;

internal class Program
{
    private static async Task Main(string[] args)
    {
        switch (7)
        {
            case 1: await BouncingSpheres(); break;
            case 2: await CheckeredSpheres(); break;
            case 3: await Earth(); break;
            case 4: await PerlinSpheres(); break;
            case 5: await Quads(); break;
            case 6: await SimpleLight(); break;
            case 7: await CornellBox(400,150,50); break;
            case 8: await CornellSmoke(); break;
            case 9: await FinalScene(400, 20, 4); break;
            case 10: await PinkTrails(imageWidth:400, samplesPerPixel: 10, maxDepth: 5); break;
            default: await FinalScene(400, 50, 6); break;
        }
    }

    private static async Task PinkTrails(int imageWidth = 400, int samplesPerPixel = 10, int maxDepth = 5)
    {
        Console.WriteLine("Simple pink horizontal trails scene");

        Material emptyMaterial = new();

        // Currently fog is purely scattering
        HittableList fogBoxes = new();

        double dx = 10000; // Along-track distance
        double dy = 100; // Cross-track distance of one cell
        double dz = 100; // "Altitude" depth of one cell

        double baseAltitude = 10000; // Altitude of the layer bottom, meters
        int nCellY = 10;
        int nLayers = 4;
        double yBase = -nCellY * dy / 2.0;

        // Make a simple box which will be filled with constant medium
        // Initially the box is axis-aligned, then translate it to the desired location
        Hittable fogBox = Quad.Box(new Vector3d(-dx/2, -dy/2, -dz/2), new Vector3d(dx/2, dy/2, dz/2), emptyMaterial);

        // The scattering probability is strictly 
        double particleRadius = 1.0e-6; // m
        double numberDensity = 1.0e8; // number per m3. 1e8: 100 per cm3; 1e12: 1 million per cm3
        double scatteringCrossSection = Math.PI * particleRadius * particleRadius; // m2 per particle
        double opticalDensity = numberDensity * scatteringCrossSection; // This is optical depth divided by distance

        for (int iLayer = 0; iLayer < nLayers; iLayer++)
        {
            double layerBase = baseAltitude + iLayer * dz;
            for (int iCellY = 0; iCellY < nCellY; iCellY++)
            {
                double localDensity = iCellY % 2 == 0 ? 0 : opticalDensity * iCellY;
                //Console.WriteLine($"{iCellY} -> {localDensity:E10}");
                Hittable translatedBox = new Translate(fogBox, new Vector3d(0, yBase + (dy * iCellY), -layerBase));
                // The material in a constant medium object is "Isotropic". Wavelength-dependent absorption
                // is defined by the the color. Scattering is currently entirely isotropic and 
                // wavelength-independent.
                fogBoxes.Add(new ConstantMedium(translatedBox, localDensity, new ColorRGB(1.0, 0.0, 1.0), "Fog"));
            }
        }

        HittableList world = new();
        world.Add(fogBoxes);
        //world.Add(solidSphere);

        world = new HittableList(new BVHNode(world));

        Camera cam = new()
        {
            ImageWidth = imageWidth,
            AspectRatio = 1.0,
            SamplesPerPixel = samplesPerPixel,
            MaxDepth = maxDepth,
            VerticalFOV = 40.0,
            LookAt = new Vector3d(0, 0, -1),
            LookFrom = new Vector3d(0, 0, 0),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.0,
            Background = new ColorRGB(0.7, 0.8, 1.0),
        };

        Hittable lights = null;
        cam.Render(world,lights);
        cam.WriteToPNG("customscene.png");
    }

    private static async Task FinalScene(int imageWidth, int samplesPerPixel, int maxDepth)
    {
        Console.WriteLine($"Final scene: {imageWidth}x{imageWidth}, {samplesPerPixel} spp, max depth {maxDepth}");
        HittableList boxes1 = new();
        Material ground = new Lambertian(new ColorRGB(0.48, 0.83, 0.53));
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

        Material light = new DiffuseLight(new ColorRGB(7, 7, 7));
        world.Add(new Quad(new Vector3d(123, 554, 147), new Vector3d(300, 0, 0), new Vector3d(0, 0, 265), light));

        Vector3d center1 = new(400, 400, 200);
        Vector3d center2 = center1 + new Vector3d(30, 0, 0);
        Material movingSphereMaterial = new Lambertian(new ColorRGB(0.7, 0.3, 0.1));
        world.Add(new Sphere(center1, center2, 50, movingSphereMaterial, "MovingSphere"));


        world.Add(new Sphere(new Vector3d(260, 150, 45), 50, new Dielectric(1.5), "InnerGlassSphere"));
        world.Add(new Sphere(new Vector3d(0, 150, 145), 50, new Metal(0.85, 1.0), "MetalSphere"));

        Hittable boundary = new Sphere(new Vector3d(360, 150, 145), 70, new Dielectric(1.5), "GlassSphereHittable");
        world.Add(boundary);
        world.Add(new ConstantMedium(boundary, 0.2, new ColorRGB(0.2, 0.4, 0.9), "InnerConstantMedium"));
        boundary = new Sphere(new Vector3d(0, 0, 0), 5000, new Dielectric(1.5), "WorldGasHittable");
        world.Add(new ConstantMedium(boundary, 0.0001, new ColorRGB(1, 1, 1), "WorldConstantMedium"));

        ITexture earthTexture = new ImageTexture("earthmap.jpg");
        world.Add(new Sphere(new Vector3d(400, 200, 400), 100, new Lambertian(earthTexture), "Earth"));

        ITexture perlinTexture = new NoiseTexture(0.2);
        world.Add(new Sphere(new Vector3d(220, 280, 300), 80, new Lambertian(perlinTexture), "PerlinSphere"));

        HittableList boxes2 = new();
        Material white = new Lambertian(new ColorRGB(0.73, 0.73, 0.73));
        int ns = 1000;
        for (int j = 0; j < ns; j++)
        {
            boxes2.Add(new Sphere(new Vector3d(165 * generator.RandomDouble(), 165 * generator.RandomDouble(), 165 * generator.RandomDouble()), 10, white, "SmallSphere"));
        }

        world.Add(new Translate(new RotateY(new BVHNode(boxes2), 15), new Vector3d(-100, 270, 395)));

        // Indicate the location of the light source(s)
        Material emptyMaterial = new();
        HittableList lights = new();
        lights.Add(new Quad(new Vector3d(123, 554, 147), new Vector3d(300, 0, 0), new Vector3d(0, 0, 265), emptyMaterial, "Light"));

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
            Background = new ColorRGB(0, 0, 0),
        };

        cam.Render(world, lights);
        cam.WriteToPNG("finalscene.png");
    }

    private static async Task CornellSmoke()
    {
        HittableList world = new();

        Material red = new Lambertian(new ColorRGB(0.65, 0.05, 0.05));
        Material white = new Lambertian(new ColorRGB(0.73, 0.73, 0.73));
        Material green = new Lambertian(new ColorRGB(0.12, 0.45, 0.15));
        Material light = new DiffuseLight(new ColorRGB(7, 7, 7));

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

        world.Add(new ConstantMedium(box1, 0.01, new ColorRGB(0, 0, 0), "Fog"));
        world.Add(new ConstantMedium(box2, 0.01, new ColorRGB(1, 1, 1), "Smoke"));

        // Indicate the location of the light source(s)
        Material emptyMaterial = new();
        HittableList lights = new();
        lights.Add(new Quad(new Vector3d(113, 554, 127), new Vector3d(330, 0, 0), new Vector3d(0, 0, 305), emptyMaterial, "Light"));

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
            Background = new ColorRGB(0, 0, 0),
        };
        cam.Render(world, lights);
        cam.WriteToPNG("cornellsmoke.png");
    }

    private static async Task CornellBox(int imageWidth = 600, int samplesPerPixel = 10, int maxDepth = 50)
    {
        HittableList world = new();

        Material red = new Lambertian(new ColorRGB(0.65, 0.05, 0.05));
        Material white = new Lambertian(new ColorRGB(0.73, 0.73, 0.73));
        Material green = new Lambertian(new ColorRGB(0.12, 0.45, 0.15));
        Material light = new DiffuseLight(new ColorRGB(15, 15, 15));
        Material aluminium = new Metal(0.85, 0.0);

        world.Add(new Quad(new Vector3d(555, 0, 0), new Vector3d(0, 0, 555), new Vector3d(0, 555, 0), green, "Left"));
        world.Add(new Quad(new Vector3d(0, 0, 0), new Vector3d(0, 555, 0), new Vector3d(0, 0, 555), red, "Right"));
        world.Add(new Quad(new Vector3d(0, 0, 0), new Vector3d(555, 0, 0), new Vector3d(0, 0, 555), white, "Lower"));
        world.Add(new Quad(new Vector3d(555, 555, 555), new Vector3d(-555, 0, 0), new Vector3d(0, 0, -555), white, "Upper"));
        world.Add(new Quad(new Vector3d(0, 0, 555), new Vector3d(555, 0, 0), new Vector3d(0, 555, 0), white, "Back"));

        // Light
        world.Add(new Quad(new Vector3d(343, 554, 332), new Vector3d(-130, 0, 0), new Vector3d(0, 0, -105), light, "Light"));

        Hittable box1 = Quad.Box(new Vector3d(0, 0, 0), new Vector3d(165, 330, 165), white);
        box1 = new RotateY(box1, 15);
        box1 = new Translate(box1, new Vector3d(265, 0, 295));
        world.Add(box1);

        //Hittable box2 = Quad.Box(new Vector3d(0, 0, 0), new Vector3d(165, 165, 165), white);
        //box2 = new RotateY(box2, -18);
        //box2 = new Translate(box2, new Vector3d(130, 0, 65));
        //world.Add(box2);
        Material glass = new Dielectric(1.5);
        world.Add(new Sphere(new Vector3d(190, 90, 190), 90, glass, "GlassSphere"));

        // Indicate the location of the light source(s)
        Material emptyMaterial = new();
        HittableList lights = new();
        lights.Add(new Quad(new Vector3d(343, 554, 332), new Vector3d(-130, 0, 0), new Vector3d(0, 0, -105), emptyMaterial, "Light"));
        lights.Add(new Sphere(new Vector3d(190, 90, 190), 90, emptyMaterial, "GlassSphere"));

        Camera cam = new()
        {
            ImageWidth = imageWidth,
            AspectRatio = 1.0,
            SamplesPerPixel = samplesPerPixel,
            MaxDepth = maxDepth,
            VerticalFOV = 40.0,
            LookAt = new Vector3d(278, 278, 0),
            LookFrom = new Vector3d(278, 278, -800),
            UpVector = new Vector3d(0.0, 1.0, 0.0),
            DefocusAngle = 0.0,
            Background = new ColorRGB(0, 0, 0),
        };
        // Test render
        bool testRender = false;
        if (testRender)
        {
            // Fire just one ray, into the center of the viewfield
            cam.SamplesPerPixel = 1;
            cam.TestRay(world, lights, cam.ImageWidth / 2, (int)((double)cam.ImageWidth / cam.AspectRatio) / 2);
            return;
        }
        cam.Render(world,lights);
        cam.WriteToPNG("cornellbox.png");
    }

    private static async Task SimpleLight()
    {
        HittableList world = new();
        Texture perlinTexture = new NoiseTexture(4.0);
        world.Add(new Sphere(new Vector3d(0, -1000, 0), 1000, new Lambertian(perlinTexture), "Ground"));
        world.Add(new Sphere(new Vector3d(0, 2, 0), 2, new Lambertian(perlinTexture), "Sphere"));

        Material diffLight = new DiffuseLight(new ColorRGB(4, 4, 4));
        world.Add(new Quad(new Vector3d(3, 1, -2), new Vector3d(2, 0, 0), new Vector3d(0, 2, 0), diffLight, "LightQuad"));
        world.Add(new Sphere(new Vector3d(0, 7, 0), 2, diffLight, "LightSphere"));

        Camera cam = new()
        {
            ImageWidth = 400,
            AspectRatio = 16.0 / 9.0,
            SamplesPerPixel = 50,
            MaxDepth = 50,
            Background = new ColorRGB(0, 0, 0),

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
        Material leftRed = new Lambertian(new ColorRGB(1.0, 0.2, 0.2));
        Material backGreen = new Lambertian(new ColorRGB(0.2, 1.0, 0.2));
        Material rightBlue = new Lambertian(new ColorRGB(0.2, 0.2, 1.0));
        Material upperOrange = new Lambertian(new ColorRGB(1.0, 0.5, 0.0));
        Material lowerTeal = new Lambertian(new ColorRGB(0.2, 0.8, 0.8));

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
            Background = new ColorRGB(0.7, 0.8, 1.0),
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
            Background = new ColorRGB(0.7, 0.8, 1.0),
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
            Background = new ColorRGB(0.7, 0.8, 1.0),
        };
        cam.Render(world);
        cam.WriteToPNG("earth.png");
    }

    private static async Task CheckeredSpheres()
    {
        HittableList world = new();
        var checker = new CheckerTexture(0.32, new ColorRGB(0.2, 0.3, 0.1), new ColorRGB(0.9, 0.9, 0.9));
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
            Background = new ColorRGB(0.7, 0.8, 1.0),
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
        Texture checker = new CheckerTexture(0.32, new ColorRGB(0.2, 0.3, 0.1), new ColorRGB(0.9, 0.9, 0.9));
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
                        var albedo = new ColorRGB(sphereGen.NextDouble() * sphereGen.NextDouble(),
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
                        var albedo = sphereGen.NextDouble() * 0.5 + 0.5;
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

        Material mat2 = new Lambertian(new ColorRGB(0.4, 0.2, 0.1));
        world.Add(new Sphere(new Vector3d(-4, 1, 0), 1.0, mat2, "BigDiffuseSphere"));

        Material mat3 = new Metal(0.6, 0.0);
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
            Background = new ColorRGB(0.7, 0.8, 1.0),
        };

        // Test render
        bool testRender = false;
        if (testRender)
        {
            // Fire just one ray, into the center of the viewfield
            cam.SamplesPerPixel = 1;
            cam.TestRay(world, null, cam.ImageWidth / 2, (int)((double)cam.ImageWidth / cam.AspectRatio) / 2);
        }
        else
        {
            cam.Render(world);
            cam.WriteToPNG("bouncing.png");
        }
    }
}