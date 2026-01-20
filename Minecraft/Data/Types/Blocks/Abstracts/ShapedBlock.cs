using BoboEngine;
using BoboEngine.Utils;
using Minecraft.Collision;

namespace Minecraft.Blocks;

public class ShapedBlock : Block
{
    public ShapedBlock() : base() { }
    public ShapedBlock(string id, string name, DynamicData data) : base(id, name, data) { }
    public ShapedBlock(string id, string name, string dataDir) : base(id, name, dataDir) { }


    DiscreteVoxelShape[] cachedShape = null;

    public override VoxelShape GetCollisionShape(Int3 pos)
    {
        if (cachedShape == null)
        {
            List<DiscreteVoxelShape> shapes = new();

            foreach (var element in model.elements)
            {
                shapes.Add(new(element.from, element.to));
            }

            cachedShape = shapes.ToArray();
        }

        return new(pos, cachedShape);
    }
}
