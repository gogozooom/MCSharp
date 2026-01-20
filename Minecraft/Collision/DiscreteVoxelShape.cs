using BoboEngine;

namespace Minecraft.Collision;

public class DiscreteVoxelShape
{
    public readonly Double3 localFrom;
    public readonly Double3 localTo;

    public DiscreteVoxelShape(Double3 localFrom, Double3 localTo)
    {
        if (localTo.x >= 0 && localTo.y >= 0 && localTo.z >= 0)
        {
            this.localFrom = localFrom;
            this.localTo = localTo;
        }
        else
        {
            throw new ArgumentException($"Need all positive sizes: {localTo}");
        }
    }

    public double Collide(Int3 worldPos, Axis axis, BoundingBox boundingBox, double movement)
    {
        if (Math.Abs(movement) < 1.0E-7)
        {
            return 0.0;
        }

        foreach (var otherAxis in OtherAxes(axis))
        {
            if (!IsAxisWithin(worldPos, otherAxis, boundingBox))
            {
                return movement;
            }
        }

        double worldAxisPos = worldPos.GetAxis(axis);

        double min = worldAxisPos + localFrom.GetAxis(axis);
        double max = worldAxisPos + localTo.GetAxis(axis);

        double bbMin = boundingBox.Min(axis);
        double bbMax = boundingBox.Max(axis);

        if (movement > 0)
        {
            if (bbMin + movement < max)
            {
                var newMovement = min - bbMax;

                if (Math.Abs(movement) >= Math.Abs(newMovement))
                    movement = Math.Min(movement, newMovement);
            }
        }
        else if (movement < 0)
        {
            if (bbMax + movement > min)
            {
                var newMovement = max - bbMin;

                if (Math.Abs(movement) >= Math.Abs(newMovement))
                    movement = Math.Max(movement, newMovement);
            }
        }

        return movement;
    }

    public static Axis[] OtherAxes(Axis axis)
    {
        return [(Axis)Maths.Mod((int)axis + 1, sizeof(Axis) - 1), (Axis)Maths.Mod((int)axis + 2, sizeof(Axis) - 1)];
    }
    public bool IsAxisWithin(Int3 worldPos, Axis axis, BoundingBox boundingBox, double skinWidth = 1.0E-6)
    {
        double worldAxisPos = worldPos.GetAxis(axis);

        double min = worldAxisPos + localFrom.GetAxis(axis) + skinWidth;
        double max = worldAxisPos + localTo.GetAxis(axis) - skinWidth;

        double bbMin = boundingBox.Min(axis) + skinWidth;
        double bbMax = boundingBox.Max(axis) - skinWidth;

        return max > bbMin && bbMax > min;
    }
    public bool IsWithin(Int3 worldPos, BoundingBox boundingBox)
    {
        for (int i = 0; i < 3; i++)
        {
            Axis axis = (Axis)i;

            if (!IsAxisWithin(worldPos, axis, boundingBox, 0)) return false;
        }

        return true;
    }
}
