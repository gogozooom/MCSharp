using BoboEngine;
using BoboEngine.Utils;
using Minecraft.Collision;
using Minecraft.World;

namespace Minecraft.Blocks;

public abstract class BaseBlock
{
    public readonly string id;
    public readonly string name;

    public BlockModel model;

    #region Init
    protected BaseBlock()
    {
        id = "minecraft:null";
        name = "NULL";

        model = BlockModel.LoadFromJson(nullData);
    }
    protected BaseBlock(string id, string name, DynamicData data)
    {
        this.id = id;
        this.name = name;

        if(data == null)
        {
            BlockModel.LoadFromJson(nullData);
            return;
        }

        MinecraftJsonManager.FullyPopulateData(ref data, "models/");

        model = BlockModel.LoadFromJson(data);
    }
    protected BaseBlock(string id, string name, string dataDir) : this(id, name, MinecraftJsonManager.GetData(dataDir)) {}

    public static DynamicData nullData => GetNullData();
    private static DynamicData _nullData;
    private static DynamicData GetNullData()
    {
        if(_nullData == null) GenerateNullData();

        return _nullData;
    }
    private static void GenerateNullData()
    {
        var data = "{{\"parent\": \"minecraft:block/cube_all\",\"textures\": {\"all\": \"minecraft:null\"}}";

        _nullData = FileParser.ParseJson("NULL", data);

        MinecraftJsonManager.FullyPopulateData(ref _nullData, "models/");
    }
    #endregion

    #region Chunk Mesh Generation
    /// <summary>
    /// This is to only be used by <see cref="Chunk"/>.cs
    /// </summary>
    public abstract void GenerateMesh(ref int elementIndex, Int3 worldBlockPosition, Int3 localBlockPosition, ref List<Float3> GenerateMesh_vertices, ref List<MeshFace> GenerateMesh_faces, ref List<Float2> GenerateMesh_textureCoords, ref List<int> GenerateMesh_textureIdIndices, ref List<Float4> GenerateMesh_occlusionLevels);

    /// <summary>
    /// Only to be used by <see cref="GenerateMesh"/>
    /// </summary>
    protected static void GM_GenerateBlockMesh(BaseBlock blockType, Int3 worldBlockPosition, Int3 localBlockPosition, ref List<Float3> vertices, ref List<MeshFace> faces, ref List<Float2> textureCoords, ref List<int> GenerateMesh_textureIdIndices, ref List<Float4> GenerateMesh_occlusionLevels, ref int elementIndex, BlockModelElement element)
    {
        if (!element.HasFaceData)
        {
            Engine.LogWarning($"'{blockType}' has elements without any faces!");
            return;
        }

        // Face Render Checks

        bool renderEastFace = false;
        bool renderWestFace = false;
        bool renderDownFace = false;
        bool renderUpFace = false;
        bool renderNorthFace = false;
        bool renderSouthFace = false;

        int eastTextureID = 0;
        int westTextureID = 0;
        int downTextureID = 0;
        int upTextureID = 0;
        int northTextureID = 0;
        int southTextureID = 0;

        // Adjacent blocks TODO: (Can be moved out of element!)
        var downBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(0, -1, 0));
        var upBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(0, 1, 0));
        var northBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(0, 0, -1));
        var southBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(0, 0, 1));
        var westBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, 0, 0));
        var eastBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, 0, 0));

        // Maybe put downBlock, upBlock, northBlock, etc... in a separate struct?

        GM_PopulateModelProperties(element.east, blockType, ref renderEastFace, ref eastTextureID,
            element,
            downBlock, upBlock, northBlock, southBlock, westBlock, eastBlock);

        GM_PopulateModelProperties(element.west, blockType, ref renderWestFace, ref westTextureID,
            element,
            downBlock, upBlock, northBlock, southBlock, westBlock, eastBlock);

        GM_PopulateModelProperties(element.down, blockType, ref renderDownFace, ref downTextureID,
            element,
            downBlock, upBlock, northBlock, southBlock, westBlock, eastBlock);

        GM_PopulateModelProperties(element.up, blockType, ref renderUpFace, ref upTextureID,
            element,
            downBlock, upBlock, northBlock, southBlock, westBlock, eastBlock);

        GM_PopulateModelProperties(element.north, blockType, ref renderNorthFace, ref northTextureID,
            element,
            downBlock, upBlock, northBlock, southBlock, westBlock, eastBlock);

        GM_PopulateModelProperties(element.south, blockType, ref renderSouthFace, ref southTextureID,
            element,
            downBlock, upBlock, northBlock, southBlock, westBlock, eastBlock);

        if (!renderEastFace && !renderWestFace && !renderDownFace && !renderUpFace && !renderNorthFace && !renderSouthFace) return;

        // -- Ambient Occlusion --

        bool shadeNorthWestUp = false;
        bool shadeNorthWestDown = false;
        bool shadeNorthEastUp = false;
        bool shadeNorthEastDown = false;
        bool shadeSouthWestUp = false;
        bool shadeSouthWestDown = false;
        bool shadeSouthEastUp = false;
        bool shadeSouthEastDown = false;

        bool shadeNorthUp = false;
        bool shadeNorthDown = false;
        bool shadeSouthUp = false;
        bool shadeSouthDown = false;
        bool shadeWestUp = false;
        bool shadeWestDown = false;
        bool shadeEastUp = false;
        bool shadeEastDown = false;

        bool shadeNorthWest = false;
        bool shadeNorthEast = false;
        bool shadeSouthEast = false;
        bool shadeSouthWest = false;

        bool shadeNorth = false;
        bool shadeSouth = false;
        bool shadeWest = false;
        bool shadeEast = false;
        bool shadeUp = false;
        bool shadeDown = false;

        if (typeof(OccludableBlock).IsAssignableFrom(blockType.GetType()))
        {
            // Corner Blocks

            var northWestUpBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, 1, -1));
            var northWestDownBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, -1, -1));
            var northEastUpBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, 1, -1));
            var northEastDownBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, -1, -1));
            var southWestUpBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, 1, 1));
            var southWestDownBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, -1, 1));
            var southEastUpBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, 1, 1));
            var southEastDownBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, -1, 1));

            // Edge Blocks

            var northUpBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(0, 1, -1));
            var northDownBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(0, -1, -1));
            var southUpBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(0, 1, 1));
            var southDownBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(0, -1, 1));
            var westUpBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, 1, 0));
            var westDownBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, -1, 0));
            var eastUpBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, 1, 0));
            var eastDownBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, -1, 0));

            var northWestBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, 0, -1));
            var northEastBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, 0, -1));
            var southEastBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(1, 0, 1));
            var southWestBlock = WorldChunkManager.GetBlockAtPosition(worldBlockPosition + new Int3(-1, 0, 1));

            shadeNorthWestUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(northWestUpBlock.block_id));
            shadeNorthWestDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(northWestDownBlock.block_id));
            shadeNorthEastUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(northEastUpBlock.block_id));
            shadeNorthEastDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(northEastDownBlock.block_id));
            shadeSouthWestUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(southWestUpBlock.block_id));
            shadeSouthWestDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(southWestDownBlock.block_id));
            shadeSouthEastUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(southEastUpBlock.block_id));
            shadeSouthEastDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(southEastDownBlock.block_id));

            shadeNorthUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(northUpBlock.block_id));
            shadeNorthDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(northDownBlock.block_id));
            shadeSouthUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(southUpBlock.block_id));
            shadeSouthDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(southDownBlock.block_id));
            shadeWestUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(westUpBlock.block_id));
            shadeWestDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(westDownBlock.block_id));
            shadeEastUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(eastUpBlock.block_id));
            shadeEastDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(eastDownBlock.block_id));

            shadeNorthWest = blockType.CanOcclude(BlockTypeManager.GetBlockType(northWestBlock.block_id));
            shadeNorthEast = blockType.CanOcclude(BlockTypeManager.GetBlockType(northEastBlock.block_id));
            shadeSouthEast = blockType.CanOcclude(BlockTypeManager.GetBlockType(southEastBlock.block_id));
            shadeSouthWest = blockType.CanOcclude(BlockTypeManager.GetBlockType(southWestBlock.block_id));

            shadeNorth = blockType.CanOcclude(BlockTypeManager.GetBlockType(northBlock.block_id));
            shadeSouth = blockType.CanOcclude(BlockTypeManager.GetBlockType(southBlock.block_id));
            shadeWest = blockType.CanOcclude(BlockTypeManager.GetBlockType(westBlock.block_id));
            shadeEast = blockType.CanOcclude(BlockTypeManager.GetBlockType(eastBlock.block_id));
            shadeUp = blockType.CanOcclude(BlockTypeManager.GetBlockType(upBlock.block_id));
            shadeDown = blockType.CanOcclude(BlockTypeManager.GetBlockType(downBlock.block_id));
        }

        // Create Vertices

        List<Float3> transformedVerts = element.GetCachedVerticies()?.ToList();

        if (transformedVerts == null)
        {
            Float3[] blockVerts = [
            new Float3(0, 0, 0), // 1
            new Float3(0, 0, 1), // 2
            new Float3(0, 1, 1), // 3
            new Float3(0, 1, 0), // 4
            new Float3(1, 0, 0), // 5
            new Float3(1, 1, 0), // 6
            new Float3(1, 1, 1), // 7
            new Float3(1, 0, 1)  // 8
            ];

            transformedVerts = new();

            BlockModelRotation rotation = element.rotation;

            foreach (var vert in blockVerts)
            {
                Float3 untransformedVert = new(vert.x == 0 ? element.from.x : element.to.x, vert.y == 0 ? element.from.y : element.to.y, vert.z == 0 ? element.from.z : element.to.z);

                Float3 transformedVert = untransformedVert;

                if (rotation) // Avoid unnecessary calculation
                {
                    transformedVert -= rotation.origin;

                    BaseVectors basisVectors;

                    switch (rotation.axis)
                    {
                        case Axis.Xaxis:
                            basisVectors = BaseVectors.FromXRotation(rotation.angle);
                            break;
                        case Axis.Zaxis:
                            basisVectors = BaseVectors.FromZRotation(rotation.angle);
                            break;
                        default:
                            basisVectors = BaseVectors.FromYRotation(-rotation.angle);
                            break;
                    }

                    Float3 scaler = new(1, 1, 1);

                    if (rotation.rescale)
                    {
                        float mult = Maths.Sec(45f * Maths.TriangleWave(rotation.angle / 45f));

                        switch (rotation.axis)
                        {
                            case Axis.Xaxis:
                                scaler = new(1, mult, mult);
                                break;
                            case Axis.Zaxis:
                                scaler = new(mult, mult, 1);
                                break;
                            default:
                                scaler = new(mult, 1, mult);
                                break;
                        }
                    }

                    transformedVert = basisVectors.TransformVector(transformedVert) * scaler;

                    transformedVert += rotation.origin;
                }

                transformedVerts.Add(transformedVert);
            }

            element.CacheVerticies(transformedVerts.ToArray());
        }


        foreach (var cachedVert in transformedVerts)
        {
            vertices.Add(cachedVert + localBlockPosition);
        }


        List<(string data, int textureId, Float4 occlusionLevels)> squareFaces = new();

        if (renderWestFace)
        {
            GM_GenerateModelFace(ref textureCoords, ref squareFaces, ref elementIndex, 
                element.west.rotation, element.west.uv, westTextureID, [1, 2, 3, 4], 1,
                0.6f, shadeNorthWestUp, shadeSouthWestUp, shadeNorthWestDown, shadeSouthWestDown, shadeWestUp, shadeWestDown, shadeNorthWest, shadeSouthWest, shadeWest);
        }

        if (renderEastFace)
        {
            GM_GenerateModelFace(ref textureCoords, ref squareFaces, ref elementIndex, 
                element.east.rotation, element.east.uv, eastTextureID, [8, 5, 6, 7], 2,
                0.6f, shadeSouthEastUp, shadeNorthEastUp, shadeSouthEastDown, shadeNorthEastDown, shadeEastUp, shadeEastDown, shadeSouthEast, shadeNorthEast, shadeEast);
        }

        if (renderDownFace)
        {
            GM_GenerateModelFace(ref textureCoords, ref squareFaces, ref elementIndex, 
                element.down.rotation, element.down.uv, downTextureID, [1, 5, 8, 2], 3,
                0.5f, shadeSouthWestDown, shadeSouthEastDown, shadeNorthWestDown, shadeNorthEastDown, shadeSouthDown, shadeNorthDown, shadeWestDown, shadeEastDown, shadeDown);
        }

        if (renderUpFace)
        {
            GM_GenerateModelFace(ref textureCoords, ref squareFaces, ref elementIndex,
                element.up.rotation, element.up.uv, upTextureID, [3, 7, 6, 4], 4,
                1, shadeNorthWestUp, shadeNorthEastUp, shadeSouthWestUp, shadeSouthEastUp, shadeNorthUp, shadeSouthUp, shadeWestUp, shadeEastUp, shadeUp);
        }

        if (renderNorthFace)
        {
            GM_GenerateModelFace(ref textureCoords, ref squareFaces, ref elementIndex,
                element.north.rotation, element.north.uv, northTextureID, [5, 1, 4, 6], 5,
                0.8f, shadeNorthEastUp, shadeNorthWestUp, shadeNorthEastDown, shadeNorthWestDown, shadeNorthUp, shadeNorthDown, shadeNorthEast, shadeNorthWest, shadeNorth);
        }

        if (renderSouthFace)
        {
            GM_GenerateModelFace(ref textureCoords, ref squareFaces, ref elementIndex,
                element.south.rotation, element.south.uv, southTextureID, [2, 8, 7, 3], 6,
                0.8f, shadeSouthWestUp, shadeSouthEastUp, shadeSouthWestDown, shadeSouthEastDown, shadeSouthUp, shadeSouthDown, shadeSouthWest, shadeSouthEast, shadeSouth);
        }

        foreach (var (data, textureId, occlusionLevels) in squareFaces)
        {
            faces.Add(new MeshFace(data));
            GenerateMesh_textureIdIndices.Add(textureId);
            GenerateMesh_occlusionLevels.Add(occlusionLevels);
        }
        elementIndex++;
    }

    /// <summary>
    /// Only to be used by <see cref="GenerateMesh"/>
    /// </summary>
    protected static void GM_PopulateModelProperties(BlockModelFace face, BaseBlock blockType, ref bool renderFace, ref int textureID,
        BlockModelElement element,
        WorldBlockData downBlock, WorldBlockData upBlock, WorldBlockData northBlock, WorldBlockData southBlock, WorldBlockData westBlock, WorldBlockData eastBlock)
    {
        if (face)
        {
            if (face.cullface != null)
            {
                string adjacentBlockId = "minecraft:air";

                switch (face.cullface)
                {
                    case "down":
                        adjacentBlockId = downBlock.block_id;
                        break;
                    case "up":
                        adjacentBlockId = upBlock.block_id;
                        break;
                    case "north":
                        adjacentBlockId = northBlock.block_id;
                        break;
                    case "south":
                        adjacentBlockId = southBlock.block_id;
                        break;
                    case "west":
                        adjacentBlockId = westBlock.block_id;
                        break;
                    case "east":
                        adjacentBlockId = eastBlock.block_id;
                        break;
                }

                renderFace = blockType.WillRenderSide(BlockTypeManager.GetBlockType(adjacentBlockId));
            }
            else renderFace = true;

            if (renderFace)
            {
                textureID = TextureManager.GetTextureID(face.texture);
            }
        }
    }

    /// <summary>
    /// ambientOcclusionLevelAmount
    /// </summary>
    private const float aOLAmount = 0.2f;

    /// <summary>
    /// Only to be used by <see cref="GenerateMesh"/>
    /// </summary>
    protected static void GM_GenerateModelFace(ref List<Float2> textureCoords, ref List<(string, int, Float4)> squareFaces, ref int elementIndex, 
        float uvRotation, UVRect uvs, int textureID, int[] vertIndices, int normalIndex,
        float baseShadeLevel, bool shadeTopLeft, bool shadeTopRight, bool shadeBottomLeft, bool shadeBottomRight, bool shadeTop, bool shadeBottom, bool shadeLeft, bool shadeRight, bool shadeFace)
    {
        switch (uvRotation)
        {
            default:
                textureCoords.AddRange([new(uvs.uMin, uvs.vMin), new(uvs.uMax, uvs.vMin), new(uvs.uMax, uvs.vMax), new(uvs.uMin, uvs.vMax)]);
                break;
            case 90f:
                textureCoords.AddRange([new(uvs.uMax, uvs.vMin), new(uvs.uMax, uvs.vMax), new(uvs.uMin, uvs.vMax), new(uvs.uMin, uvs.vMin)]);
                break;
            case 180f:
                textureCoords.AddRange([new(uvs.uMax, uvs.vMax), new(uvs.uMin, uvs.vMax), new(uvs.uMin, uvs.vMin), new(uvs.uMax, uvs.vMin)]);
                break;
            case 270f:
                textureCoords.AddRange([new(uvs.uMin, uvs.vMax), new(uvs.uMin, uvs.vMin), new(uvs.uMax, uvs.vMin), new(uvs.uMax, uvs.vMax)]);
                break;
        }

        var uvO = textureCoords.Count;

        var vO = elementIndex * 8;

        // Corner Shading
        float topLeftLightValue = 1; if (shadeTopLeft) topLeftLightValue -= aOLAmount;
        float topRightLightValue = 1; if (shadeTopRight) topRightLightValue -= aOLAmount;
        float bottomLeftLightValue = 1; if (shadeBottomLeft) bottomLeftLightValue -= aOLAmount;
        float bottomRightLightValue = 1; if (shadeBottomRight) bottomRightLightValue -= aOLAmount;

        // Edge Shading
        if (shadeTop)
        {
            topLeftLightValue -= aOLAmount;
            topRightLightValue -= aOLAmount;
        }
        if (shadeBottom)
        {
            bottomLeftLightValue -= aOLAmount;
            bottomRightLightValue -= aOLAmount;
        }
        if (shadeLeft)
        {
            topLeftLightValue -= aOLAmount;
            bottomLeftLightValue -= aOLAmount;
        }
        if (shadeRight)
        {
            topRightLightValue -= aOLAmount;
            bottomRightLightValue -= aOLAmount;
        }
        if (shadeFace)
        {
            topLeftLightValue -= aOLAmount;
            topRightLightValue -= aOLAmount;
            bottomLeftLightValue -= aOLAmount;
            bottomRightLightValue -= aOLAmount;
        }

        squareFaces.Add(($"f {vertIndices[0] + vO}/{uvO - 3}/{normalIndex} " +
                          $"{vertIndices[1] + vO}/{uvO - 2}/{normalIndex} " +
                          $"{vertIndices[2] + vO}/{uvO - 1}/{normalIndex} " +
                          $"{vertIndices[3] + vO}/{uvO}/{normalIndex}",
                          textureID,
                          new (baseShadeLevel * bottomLeftLightValue, baseShadeLevel * bottomRightLightValue, baseShadeLevel * topRightLightValue, baseShadeLevel * topLeftLightValue)
                          ));
    }

    /// <summary>
    /// Used for face culling optimisation
    /// </summary>
    public bool WillRenderSide(BaseBlock adjasentBlock)
    {
        if(this is CullableBlock)
        {
            if (typeof(CullerBlock).IsAssignableFrom(adjasentBlock.GetType()))
            {
                return false;
            }
            if (typeof(SelfCullingBlock).IsAssignableFrom(GetType()) && id == adjasentBlock.id)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Used for ambient occlusion
    /// </summary>
    public bool CanOcclude(BaseBlock otherBlock)
    {
        if (this is OccludableBlock)
        {
            if (typeof(OccluderBlock).IsAssignableFrom(otherBlock.GetType()))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    internal static readonly DiscreteVoxelShape[] DefaultShape = [new(Int3.zero, Float3.one)];

    public virtual VoxelShape GetCollisionShape(Int3 pos) => model.elements.Length > 0 ? new(pos, DefaultShape) : null;


    public override string ToString()
    {
        return id;
    }
}