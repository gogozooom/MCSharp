using BoboEngine;

namespace Minecraft.Collision;

public class VoxelShape(Int3 position, DiscreteVoxelShape[] shapes)
{
    public readonly Int3 position = position;
    public readonly DiscreteVoxelShape[] shapes = shapes;

    public double Collide(Axis axis, BoundingBox boundingBox, double distance)
    {
        foreach (var shape in shapes)
        {
            distance = shape.Collide(position, axis, boundingBox, distance);
        }

        return distance;
    }

    public double[] GetCoords(Axis axis)
    {
        List<double> coords = new();

        var worldAxisPos = position.GetAxis(axis);

        foreach(var shape in shapes)
        {
            coords.Add(worldAxisPos + shape.localTo.GetAxis(axis));
        }

        return coords.ToArray();
    }

    public bool IsWithin(BoundingBox boundingBox)
    {
        foreach(var shape in shapes)
        {
            if (shape.IsWithin(position, boundingBox)) return true;
        }
        return false;
    }
}
