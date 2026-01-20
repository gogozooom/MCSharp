using BoboEngine;
using BoboEngine.Utils;
using Minecraft.Collision;

namespace Minecraft.Blocks;

public class LeafBlock : Block, CullableBlock, OccludableBlock, OccluderBlock
{
    public LeafBlock() : base() { }
    public LeafBlock(string id, string name, DynamicData data) : base(id, name, data) { }
    public LeafBlock(string id, string name, string dataDir) : base(id, name, dataDir) { }
}