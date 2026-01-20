using BoboEngine.Utils;

namespace Minecraft.Blocks;

internal class Slab : ShapedBlock, OccludableBlock, OccluderBlock, CullableBlock
{
    public Slab() : base() { }
    public Slab(string id, string name, DynamicData data) : base(id, name, data) { }
    public Slab(string id, string name, string dataDir) : base(id, name, dataDir) { }
}
