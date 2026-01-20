using BoboEngine;
using Minecraft.Blocks;
using System.Runtime.InteropServices;

namespace Minecraft.World;
public class Chunk : ObjectBehavior
{
    public Int3 chunkPosition { get; private set; }

    private readonly ChunkData _chunkData = new(WorldBlockData.AIR);
    
    public bool newMeshGenerated { get; private set; }
    public bool generatingMesh { get; private set; }
    private int _meshGenerationTaskID = 0;

    public void InitChunk(Int3 position)
    {
        currentJobID = -1;

        chunkPosition = position;
        transform.position = chunkPosition * 16;
    }

    public override void Start()
    {
        _connectedMesh = gameObject.GetComponent<ChunkMesh>();

        if (!_connectedMesh)
        {
            _connectedMesh = gameObject.AddComponent<ChunkMesh>();
            _connectedMesh.material = WorldChunkManager.meshMaterial;
        }
    }
    public void ClearChunk() => _chunkData.Clear();

    private ChunkMesh _connectedMesh;
    public int currentJobID { get; private set; }

    private List<Float3> GenerateMesh_vertices;
    private List<ChunkFaceInfo> GenerateMesh_faces;
    private List<Float2> GenerateMesh_textureCoords;
    private List<Float3> GenerateMesh_normals = [
        BlockFace.GetNormal(BlockDirection.WEST),
            BlockFace.GetNormal(BlockDirection.EAST),
            BlockFace.GetNormal(BlockDirection.DOWN),
            BlockFace.GetNormal(BlockDirection.UP),
            BlockFace.GetNormal(BlockDirection.NORTH),
            BlockFace.GetNormal(BlockDirection.SOUTH),
            ];

    private object _lock = new();

    /// <summary>
    /// USED BY <see cref="WorldChunkManager"/>! <br/>
    /// <br/>
    /// Updates the visual mesh.
    /// </summary>
    public bool GenerateMesh()
    {
        lock (_lock)
        {
            if (generatingMesh)
            {
                //Program.LogWarning($"id({currentJobID}) {this} Already generating mesh!");
                return false;
            }
        }
        //Program.LogMessage($"id({currentJobID}) Started {this}");


        int taskID;

        taskID = ++_meshGenerationTaskID;

        generatingMesh = true;
        newMeshGenerated = false;

        GenerateMesh_vertices = new();
        GenerateMesh_faces = new();
        GenerateMesh_textureCoords = new();

        int elementIndex = 0;
        int blockI = 0;

        for (int y = 0; y < ChunkData.CHUNK_SIZE; y++)
        {
            for (int z = 0; z < ChunkData.CHUNK_SIZE; z++)
            {
                for (int x = 0; x < ChunkData.CHUNK_SIZE; x++)
                {
                    lock (_lock)
                    {
                        // Outdated Mesh Data Check
                        if (taskID != _meshGenerationTaskID || generatingMesh == false)
                        {
                            //Program.LogWarning($"id({currentJobID}) {this} Outdated Blocks!");
                            generatingMesh = false;
                            return false;
                        }
                    }

                    WorldBlockData block = _chunkData.GetBlockAtIndex(blockI++);

                    if (block.block_id == "minecraft:air" || block.block_id == "minecraft:void") continue;

                    Int3 localBlockPosition = new(x, y, z);
                    BaseBlock blockType = BlockTypeManager.GetBlockType(block.block_id);

                    blockType.GenerateMesh(ref elementIndex, LocalPositionToWorld(localBlockPosition), localBlockPosition, ref GenerateMesh_vertices, ref GenerateMesh_faces, ref GenerateMesh_textureCoords);
                }
            }
        }

        generatingMesh = false;
        newMeshGenerated = true;

        //Program.LogMessage($"id({currentJobID}) Finished {this}");
        return true;
    }

    public void AssignJobID(int id) => currentJobID = id;
    public void CancelGeneration()
    {
        generatingMesh = false;
        currentJobID = -1;
    }

    public void LoadGeneratedMesh()
    {
        if (!newMeshGenerated || generatingMesh)
        {
            //Program.LogWarning($"Cannot Load Chunk {this}! newMeshGenerated = {newMeshGenerated} | generatingMesh = {generatingMesh}");
            return;
        }

        _connectedMesh.DeleteMesh();
        _connectedMesh.LoadRawData(GenerateMesh_vertices.ToArray(), GenerateMesh_faces.ToArray(), GenerateMesh_normals.ToArray(), GenerateMesh_textureCoords.ToArray());
        GenerateMesh_vertices = new();
        GenerateMesh_faces = new();
        GenerateMesh_textureCoords = new();

        newMeshGenerated = false;
        currentJobID = -1;
    }

    public WorldBlockData GetBlockAtLocalPosition(Int3 localPosition)
    {
        if (!IsLocalPositionValid(localPosition))
        {
            Engine.LogWarning("GetBlockAtPosition() Position out of range!");
            return WorldBlockData.AIR;
        }

        return _chunkData.GetBlockAtLocalPosition(localPosition);
    }
    public WorldBlockData GetBlockAtLocalPositionUnsafe(Int3 localPosition) => _chunkData.GetBlockAtLocalPosition(localPosition);

    public void SetBlockAtLocalPosition(Int3 localPosition, WorldBlockData block)
    {
        if (!IsLocalPositionValid(localPosition))
        {
            Engine.LogWarning("SetBlockAtLocalPosition() Cannot set block! Out of bounds!");
            return;
        }

        _chunkData.SetBlockAtLocalPosition(localPosition, block);
    }
    public void SetBlockAtLocalPositionUnsafe(Int3 localPosition, WorldBlockData block) => _chunkData.SetBlockAtLocalPosition(localPosition, block);

    public Int3 WorldPositionToLocal(Int3 worldPosition) => worldPosition - (chunkPosition * 16);
    public Int3 LocalPositionToWorld(Int3 localPosition) => localPosition + (chunkPosition * 16);

    public bool IsLocalPositionValid(Int3 localPosition)
    {
        if (localPosition.x < 0 || localPosition.x >= 16)
        {
            return false;
        }
        if (localPosition.y < 0 || localPosition.y >= 16)
        {
            return false;
        }
        if (localPosition.z < 0 || localPosition.z >= 16)
        {
            return false;
        }

        return true;
    }

    public override string ToString()
    {
        return base.ToString() + " " + chunkPosition;
    }
}

public struct BlockRaycastHit
{
    public WorldBlockData blockHit;
    public BlockFace blockFace;
    public Double3 hitPosition;
    public Int3 blockPosition;

    public BlockRaycastHit()
    {
        blockHit = WorldBlockData.AIR;
        blockPosition = new();
        blockFace = new();
        hitPosition = new();
    }
    public BlockRaycastHit(WorldBlockData blockHit, Int3 blockPosition, BlockFace blockFace, Double3 hitPosition)
    {
        this.blockHit = blockHit;
        this.blockPosition = blockPosition;
        this.blockFace = blockFace;
        this.hitPosition = hitPosition;
    }

    public static implicit operator bool(BlockRaycastHit hit) => hit.blockHit.block_id != "minecraft:air";

    public override string ToString()
    {
        return $"[{blockHit}] '{blockFace}' '{hitPosition}'";
    }
}