using BoboEngine;
using BoboEngine.Utils;
using Minecraft.World;

namespace Minecraft.Blocks;

public class Block : BaseBlock
{
    public Block() : base() { }
    public Block(string id, string name, DynamicData data) : base(id, name, data) { }
    public Block(string id, string name, string dataDir) : base(id, name, dataDir) { }

    public override void GenerateMesh(ref int elementIndex, Int3 worldBlockPosition, Int3 localBlockPosition, ref List<Float3> GenerateMesh_vertices, ref List<ChunkFaceInfo> GenerateMesh_faces, ref List<Float2> GenerateMesh_textureCoords)
    {
        // Flipped order to allow block overlays to render over
        for (int i = model.elements.Length - 1; i >= 0; i--)
        {
            GM_GenerateBlockMesh(this, worldBlockPosition, localBlockPosition, ref GenerateMesh_vertices, ref GenerateMesh_faces, ref GenerateMesh_textureCoords, ref elementIndex, model.elements[i]);
        }
    }
}
