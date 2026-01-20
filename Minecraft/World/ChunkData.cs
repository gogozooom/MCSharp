using BoboEngine;

namespace Minecraft.World;

public class ChunkData
{
    public const int CHUNK_SIZE = 16;
    private WorldBlockData[] _data = new WorldBlockData[CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];

    public ChunkData(){}
    public ChunkData(WorldBlockData type)
    {
        Clear(type);
    }

    public void Clear(WorldBlockData? block = null)
    {
        WorldBlockData data = block == null ? WorldBlockData.AIR : block.Value;

        Parallel.For(0, _data.Length, (i, state) =>
        {
            _data[i] = data;
        });
    }

    public WorldBlockData GetBlockAtLocalPosition(Int3 localPosition) => _data[Int3ToIndex(localPosition)];
    public WorldBlockData GetBlockAtIndex(int index) => _data[index];
    
    public void SetBlockAtLocalPosition(Int3 localPosition, WorldBlockData block) => _data[Int3ToIndex(localPosition)] = block;
    public void SetBlockAtIndex(int index, WorldBlockData block) => _data[index] = block;


    public static int Int3ToIndex(Int3 p) => IntsToIndex(p.x, p.y, p.z);
    public static int IntsToIndex(int x, int y, int z) => x + 16 * z + 256 * y;
}
