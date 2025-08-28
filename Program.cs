using System.Net.NetworkInformation;
using System.Numerics;

namespace RayTrace;

internal class Program
{
    private static void Main(string[] args)
    {
        // Set up the world
        HittableList world = new();

        // Set up the materials
        Material materialGround = new Lambertian(new Vector3d(0.8, 0.8, 0.0));
        Material materialCenter = new Lambertian(new Vector3d(0.1, 0.2, 0.5));
        Material materialLeft = new Metal(new Vector3d(0.8, 0.8, 0.8));
        Material materialRight = new Metal(new Vector3d(0.8, 0.6, 0.2));

        // "Ground"
        world.Add(new Sphere(new Vector3d(0.0, -100.5, -1.0), 100.0, materialGround));
        // Sphere directly in front of the camera
        world.Add(new Sphere(new Vector3d( 0.0, 0.0, -1.2), 0.5, materialCenter));
        world.Add(new Sphere(new Vector3d(-1.0, 0.0, -1.0), 0.5, materialLeft));
        world.Add(new Sphere(new Vector3d(+1.0, 0.0, -1.0), 0.5, materialRight));

        Camera cam = new()
        {
            ImageWidth = 400,
            AspectRatio = 16.0 / 9.0,
            SamplesPerPixel = 100,
            MaxDepth = 50
        };

        cam.Render(world);
        cam.WriteToFile("image.ppm");
    }
}