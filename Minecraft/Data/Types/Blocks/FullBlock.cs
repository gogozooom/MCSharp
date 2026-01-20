using BoboEngine.Utils;

namespace Minecraft.Blocks;

public class FullBlock : Block, OccludableBlock, OccluderBlock, CullableBlock, CullerBlock
{
    public FullBlock() : base() { }
    public FullBlock(string id, string name, DynamicData data) : base(id, name, data) { }
    public FullBlock(string id, string name, string dataDir) : base(id, name, dataDir) { }
}