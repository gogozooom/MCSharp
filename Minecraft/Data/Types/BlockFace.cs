using BoboEngine;
using System.Collections.Immutable;

namespace Minecraft;

public struct BlockFace
{
    private static readonly ImmutableList<Axis> YXZ_AXIS_ORDER = [Axis.Yaxis, Axis.Xaxis, Axis.Zaxis];
    private static readonly ImmutableList<Axis> YZX_AXIS_ORDER = [Axis.Yaxis, Axis.Zaxis, Axis.Xaxis];

    public BlockDirection direction;

    public BlockFace(BlockDirection direction = BlockDirection.SELF)
    {
        this.direction = direction;
    }

    public BlockFace(string direction)
    {
        switch (direction.ToLower())
        {
            case "down":
                this.direction = BlockDirection.DOWN;
                break;
            case "up":
                this.direction = BlockDirection.UP;
                break;
            case "north":
                this.direction = BlockDirection.NORTH;
                break;
            case "south":
                this.direction = BlockDirection.SOUTH;
                break;
            case "west":
                this.direction = BlockDirection.WEST;
                break;
            case "east":
                this.direction = BlockDirection.EAST;
                break;
            default:
            case "self":
                this.direction = BlockDirection.SELF;
                break;

        }
    }

    public Float3 GetNormal()
    {
        return GetNormal(direction);
    }

    public static Float3 GetNormal(BlockDirection direction)
    {
        switch (direction)
        {
            case BlockDirection.NORTH:
                return new(0, 0, -1);
            case BlockDirection.DOWN:
                return new(0, -1, 0);
            case BlockDirection.WEST:
                return new(-1, 0, 0);
            case BlockDirection.SOUTH:
                return new(0, 0, 1);
            case BlockDirection.UP:
                return new(0, 1, 0);
            case BlockDirection.EAST:
                return new(1, 0, 0);
            default:
                return new(0, 0, 0);
        }
    }

    public static BlockDirection Opposite(BlockDirection blockDirection)
    {
        switch (blockDirection)
        {
            case BlockDirection.UP:
                return BlockDirection.DOWN;
            case BlockDirection.DOWN:
                return BlockDirection.UP;
            case BlockDirection.NORTH:
                return BlockDirection.SOUTH;
            case BlockDirection.SOUTH:
                return BlockDirection.NORTH;
            case BlockDirection.EAST:
                return BlockDirection.WEST;
            case BlockDirection.WEST:
                return BlockDirection.EAST;
            default:
                return BlockDirection.SELF;
        }
    }

    public static ImmutableList<Axis> AxisStepOrder(Double3 movement)
    {
        return Math.Abs(movement.x) < Math.Abs(movement.z) ? YZX_AXIS_ORDER : YXZ_AXIS_ORDER;
    }

    public override string ToString()
    {
        return direction.ToString();
    }
}
public enum BlockDirection
{
    SELF,
    DOWN,
    EAST,
    NORTH,
    SOUTH,
    UP,
    WEST
}