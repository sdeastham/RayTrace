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
        world.Add(new Sphere(new Vector3(0.0f, 0.0f, -1.0f), 0.5f));
        // "Ground"
        world.Add(new Sphere(new Vector3(0.0f, -100.5f, -1.0f), 100.0f));

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