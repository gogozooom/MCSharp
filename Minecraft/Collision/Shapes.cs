using BoboEngine;

namespace Minecraft.Collision;

public static class Shapes
{
    public static double Collide(Axis axis, BoundingBox boundingBox, List<VoxelShape> shapes, double distance)
    {
        if (Math.Abs(distance) < 1.0E-7) return 0.0;

        foreach (var shape in shapes)
        {
            distance = shape.Collide(axis, boundingBox, distance);
        }

        return distance;
    }
}
