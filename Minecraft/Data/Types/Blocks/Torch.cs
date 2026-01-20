using BoboEngine;
using BoboEngine.Utils;
using Minecraft.Collision;

namespace Minecraft.Blocks;

public class Torch : Block
{
    public Torch() : base() { }
    public Torch(string id, string name, DynamicData data) : base(id, name, data) { }
    public Torch(string id, string name, string dataDir) : base(id, name, dataDir) { }

    public override VoxelShape GetCollisionShape(Int3 pos) => null;
}