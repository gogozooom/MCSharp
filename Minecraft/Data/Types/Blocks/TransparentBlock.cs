using BoboEngine;
using BoboEngine.Utils;

namespace Minecraft.Blocks;

public class TransparentBlock : BaseBlock, CullableBlock, SelfCullingBlock
{
    public TransparentBlock() : base() { }
    public TransparentBlock(string id, string name, DynamicData data) : base(id, name, data) { }
    public TransparentBlock(string id, string name, string dataDir) : base(id, name, dataDir) { }

    public override void GenerateMesh(ref int elementIndex, Int3 worldBlockPosition, Int3 localBlockPosition,
        ref List<Float3> GenerateMesh_vertices, ref List<MeshFace> GenerateMesh_faces, ref List<Float2> GenerateMesh_textureCoords, ref List<int> GenerateMesh_textureIdIndices, ref List<Float4> GenerateMesh_occlusionLevels)
    {
        foreach (var element in model.elements)
        {
            GM_GenerateBlockMesh(this, worldBlockPosition, localBlockPosition, ref GenerateMesh_vertices, ref GenerateMesh_faces, ref GenerateMesh_textureCoords, ref GenerateMesh_textureIdIndices, ref GenerateMesh_occlusionLevels, ref elementIndex, element);
        }
    }
}