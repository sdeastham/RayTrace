using System.Net.NetworkInformation;
using System.Numerics;

namespace RayTrace;

internal class Program
{
    private static void Main(string[] args)
    {
        // Set up the world
        HittableList world = new();
        // Sphere directly in front of the camera
        world.Add(new Sphere(new Vector3d(0.0, 0.0, -1.0), 0.5));
        // "Ground"
        world.Add(new Sphere(new Vector3d(0.0, -100.5, -1.0), 100.0));

        Camera cam = new()
        {
            ImageWidth = 400,
            AspectRatio = 16.0 / 9.0,
            SamplesPerPixel = 100
        };

        cam.Render(world);
        cam.WriteToFile("image.ppm");
    }
}