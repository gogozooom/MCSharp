using BoboEngine;
using Minecraft.Entites;

namespace Minecraft;

public class BoundingBox
{
    public readonly double minX;
    public readonly double minY;
    public readonly double minZ;
    public readonly double maxX;
    public readonly double maxY;
    public readonly double maxZ;

    public BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
    {
        this.minX = Math.Min(minX, maxX);
        this.minY = Math.Min(minY, maxY);
        this.minZ = Math.Min(minZ, maxZ);
        this.maxX = Math.Max(minX, maxX);
        this.maxY = Math.Max(minY, maxY);
        this.maxZ = Math.Max(minZ, maxZ);
    }
    public BoundingBox(Int3 pos)
    {
        minX = pos.x;
        minY = pos.y;
        minZ = pos.z;
        maxX = pos.x + 1;
        maxY = pos.y + 1;
        maxZ = pos.z + 1;
    }
    public BoundingBox(Float3 begin, Float3 end)
    {
        minX = begin.x;
        minY = begin.y;
        minZ = begin.z;
        maxX = end.x;
        maxY = end.y;
        maxZ = end.z;
    }
    public BoundingBox(Float3 position, Double3 scale)
    {
        Double3 hScale = scale / 2;

        minX = position.x - hScale.x;
        minY = position.y;
        minZ = position.z - hScale.z;
        maxX = position.x + hScale.x;
        maxY = position.y + scale.y;
        maxZ = position.z + hScale.z;
    }
    public BoundingBox(Entity entity) : this(entity.position, entity.boundingBoxSize) {}

    public BoundingBox ExpandTowards(Double3 delta)
    {
        return ExpandTowards(delta.x, delta.y, delta.z);
    }

    public BoundingBox ExpandTowards(double xa, double ya, double za)
    {
        double minX = this.minX;
        double minY = this.minY;
        double minZ = this.minZ;
        double maxX = this.maxX;
        double maxY = this.maxY;
        double maxZ = this.maxZ;

        if (xa < 0.0)
        {
            minX += xa;
        }
        else if (xa > 0.0)
        {
            maxX += xa;
        }

        if (ya < 0.0)
        {
            minY += ya;
        }
        else if (ya > 0.0)
        {
            maxY += ya;
        }

        if (za < 0.0)
        {
            minZ += za;
        }
        else if (za > 0.0)
        {
            maxZ += za;
        }

        return new BoundingBox(minX, minY, minZ, maxX, maxY, maxZ);
    }
    public BoundingBox Deflate(double xSubstract, double ySubtract, double zSubtract)
    {
        return Inflate(-xSubstract, -ySubtract, -zSubtract);
    }

    public BoundingBox Deflate(double amount)
    {
        return Inflate(-amount);
    }
    public BoundingBox Inflate(double xAdd, double yAdd, double zAdd)
    {
        double minX = this.minX - xAdd;
        double minY = this.minY - yAdd;
        double minZ = this.minZ - zAdd;
        double maxX = this.maxX + xAdd;
        double maxY = this.maxY + yAdd;
        double maxZ = this.maxZ + zAdd;
        return new BoundingBox(minX, minY, minZ, maxX, maxY, maxZ);
    }

    public BoundingBox Inflate(double amountToAddInAllDirections)
    {
        return Inflate(amountToAddInAllDirections, amountToAddInAllDirections, amountToAddInAllDirections);
    }


    public BoundingBox Move(Double3 pos)
    {
        return Move(pos.x, pos.y, pos.z);
    }
    public BoundingBox Move(double xa, double ya, double za)
    {
        return new BoundingBox(minX + xa, minY + ya, minZ + za, maxX + xa, maxY + ya, maxZ + za);
    }

    public double Min(Axis axis)
    {
        return new Double3(minX, minY, minZ).GetAxis(axis);
    }

    public double Max(Axis axis)
    {
        return new Double3(maxX, maxY, maxZ).GetAxis(axis);
    }
}
