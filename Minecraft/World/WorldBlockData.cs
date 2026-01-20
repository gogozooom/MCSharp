namespace Minecraft.World;

public struct WorldBlockData
{
    public string block_id;

    public static readonly WorldBlockData AIR = new();

    public WorldBlockData()
    {
        block_id = "minecraft:air";
    }
    public WorldBlockData(string block_id)
    {
        this.block_id = block_id;
    }

    public override string ToString()
    {
        return block_id;
    }
}