using BoboEngine;
using BoboEngine.Shaders;
using ConsoleCommand;
using Minecraft.Blocks;
using Minecraft.Collision;
using Minecraft.Debugging;
using Minecraft.Entites;

namespace Minecraft.World;
public static class WorldChunkManager
{
    public static Material meshMaterial;

    static readonly Dictionary<Int3, Chunk> _chunks = new();

    static readonly List<Chunk> _readyChunks = new();
    static int _nextJobId = 0;

    [OnEngineInitialize]
    public static void Init()
    {
        WindowManager.update += Update;
    }

    private static void Update()
    {
        if(_readyChunks.Count > 0)
        {
            foreach (var chunk in _readyChunks.ToList())
            {
                if (chunk == null) continue;
                chunk.LoadGeneratedMesh();

                _readyChunks.Remove(chunk);
            }

            // Remove Null Chunks
            for (int i = _readyChunks.Count - 1; i >= 0; i--)
            {
                if (_readyChunks[i] == null) _readyChunks.RemoveAt(i);
            }
        }
    }

    private static object _lock = new();
    public static void CreateChunkGenerationJob(List<Chunk> c)
    {
        List<Chunk> chunks = new();
        List<Chunk> regenerate = new();

        var profiler = new Profiler();
         
        profiler.Push("Pre-Check");

        var jobID = _nextJobId++;

        lock (_lock)
        {
            foreach (var chunk in c)
            {
                if (regenerate.Contains(chunk) || chunks.Contains(chunk))
                {
                    Engine.LogError($"CreateChunkGenerationJob() - {chunk} Duplicate exists, this should not happen!");
                    continue;
                }

                if (chunk.currentJobID != -1 || chunk.generatingMesh)
                {
                    chunk.CancelGeneration();
                }

                chunk.AssignJobID(jobID);
                chunks.Add(chunk);
            }
        }

        profiler.Pop();

        Task.Run(() =>
        {
            profiler.Push("Generate Meshs");
            Parallel.ForEach(chunks, chunk =>
            {
                bool result = chunk.GenerateMesh();
                if (!result)
                {
                    //Program.LogWarning($"{chunk} Ended prematurely");
                    chunks.Remove(chunk);
                }
            });
            profiler.Pop();

            //profiler.PrintResults($"id({jobID})");

            _readyChunks.AddRange(regenerate);
            _readyChunks.AddRange(chunks);
        });
    }

    public static void GenerateTestChunk()
    {
        if (meshMaterial == null)
        {
            string shaderID = "worldShader";
            ShaderManager.EnsureShader(shaderID, "Shader/worldShader.vert", "Shader/worldShader.frag");

            meshMaterial = new Material(shaderID, TextureManager.blockTextureArray, blendMode: BlendMode.Disable);
        }

        CreateChunkArea(new(-13, -4, -13), new(13, 0, 13));
        //CreateChunkArea(new(-1, -1, -1), new(1, 1, 1));

        int x = 0;
        int z = 0;
        float rowCount = MathF.Sqrt(BlockTypeManager.blockData.Count);

        foreach (var block in BlockTypeManager.blockData)
        {
            switch (block.Key)
            {
                case "minecraft:bedrock":
                case "minecraft:air":
                case "minecraft:null":
                    continue;
            }

            SetBlock(new(x * 2, 0, z * 2), new(block.Key), false);

            x++;

            if(x >= rowCount)
            {
                z++;
                x = 0;
            }
        }

        FillCheckerPattern(new(-24, -20, -24), new(24, -1, 24), "minecraft:black_wool", "minecraft:white_wool", false);

        //FillBlocks(new(-25, -4, -25), new(25, -2, 25), "minecraft:dirt", false);
        //FillBlocks(new(-25, -20, -25), new(25, -5, 25), "minecraft:stone", false);
        FillBlocks(new(-100, -21, -100), new(100, -21, 100), "minecraft:bedrock", false);

        //FillBlocks(new(-25, -20, 25), new(25, 15, 25), "minecraft:glass", false);
        //FillBlocks(new(-25, -20, -25), new(25, 15, -25), "minecraft:glass", false);
        //FillBlocks(new(25, -20, -25), new(25, 15, 25), "minecraft:glass", false);
        //FillBlocks(new(-25, -20, -25), new(-25, 15, 25), "minecraft:glass", false);
        //FillBlocks(new(-25, 15, -25), new(25, 15, 25), "minecraft:glass", false);

        CreateChunkGenerationJob([.. _chunks.Values]);
    }
    public static bool IsBlockInEntity(Int3 blockPos)
    {
        throw new NotImplementedException();
        /*
        foreach (var obj in SceneManager.currentScene.objects)
        {
            var entity = obj.GetComponent<Entity>();
            if (!entity) continue;

            var bb = entity.GetBoundingBox();

            if (IsBlockInBoundingBox(blockPos, bb)) return true;
        }
        return false;
        */
    }
    public static bool IsBlockInBoundingBox(Int3 blockPos, string block_id, BoundingBox bb)
    {
        var blockType = BlockTypeManager.GetBlockType(block_id);

        if (blockType == null) return false;

        var collision = blockType.GetCollisionShape(blockPos);

        if (collision == null) return false;

        return collision.IsWithin(bb);
    }
    public static List<Int3> PositionsInBounds(BoundingBox boundingBox)
    {
        int maxX = Maths.Floor(boundingBox.maxX);
        int maxY = Maths.Floor(boundingBox.maxY);
        int maxZ = Maths.Floor(boundingBox.maxZ);
        int minX = Maths.Floor(boundingBox.minX);
        int minY = Maths.Floor(boundingBox.minY);
        int minZ = Maths.Floor(boundingBox.minZ);

        //Program.Log($"Max: {new Int3(maxX, maxY, maxZ)} Min: {new Int3(minX, minY, minZ)}");

        List<Int3> positions = new();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Int3 pos = new(x, y, z);

                    WorldBlockData block = GetBlockAtPosition(pos);

                    BaseBlock blockType = BlockTypeManager.GetBlockType(block.block_id);

                    if (block.block_id == "minecraft:air") continue;

                    positions.Add(pos);
                }
            }
        }

        return positions;
    }
    public static bool NoCollision(Entity source, BoundingBox boundingBox)
    {
        var results = GetBlockCollisions(source, boundingBox);

        return results.Count == 0;
    }
    public static List<VoxelShape> GetBlockCollisions(Entity source, BoundingBox boundingBox)
    {
        List<VoxelShape> voxelShapes = new();

        foreach (var pos in PositionsInBounds(boundingBox))
        {
            WorldBlockData block = GetBlockAtPosition(pos);

            BaseBlock blockType = BlockTypeManager.GetBlockType(block.block_id);

            VoxelShape voxelShape = blockType.GetCollisionShape(pos);

            if (voxelShape == null) continue;

            if (!voxelShape.IsWithin(boundingBox)) continue;

            voxelShapes.Add(voxelShape);
        }

        var position = new Double3((boundingBox.maxX + boundingBox.minX)/2, boundingBox.minY, (boundingBox.maxZ + boundingBox.minZ)/2);

        // Sorts the colliders by distance (Probably not necessary)
        //voxelShapes = voxelShapes.OrderBy(x => (x.position + Float3.one/2 - position).LengthSquared()).ToList();

        return voxelShapes;
    }
    public static List<VoxelShape> GetEntityCollisions(Entity source, BoundingBox boundingBox)
    {
        return new();
    }

    public static Int3? FindSupportingBlock(Entity source, BoundingBox box)
    {
        Int3? mainSupport = null;
        double mainSupportDistance = double.MaxValue;

        foreach (var pos in PositionsInBounds(box))
        {
            double distance = (pos - source.transform.position - Float3.one/2).LengthSquared();
            if (distance < mainSupportDistance || distance == mainSupportDistance && (mainSupport == null || mainSupport.Value.CompareTo(pos) < 0))
            {
                mainSupport = pos;
                mainSupportDistance = distance;
            }
        }

        return mainSupport;
    }

    private static void CreateChunkArea(Int3 p1, Int3 p2)
    {
        int minX = p1.x;
        int maxX = p2.x;

        if (p1.x > p2.x)
        {
            minX = p2.x;
            maxX = p1.x;
        }

        int minY = p1.y;
        int maxY = p2.y;

        if (p1.y > p2.y)
        {
            minY = p2.y;
            maxY = p1.y;
        }

        int minZ = p1.z;
        int maxZ = p2.z;

        if (p1.z > p2.z)
        {
            minZ = p2.z;
            maxZ = p1.z;
        }

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    CreateChunkObject(new(x, y, z));
                }
            }
        }
    }
    private static Chunk CreateChunkObject(Int3 chunkPos)
    {
        var newChunk = new GameObject("Chunk " + chunkPos).AddComponent<Chunk>();

        newChunk.InitChunk(chunkPos);

        _chunks.Add(chunkPos, newChunk);

        return newChunk;
    }
    public static Chunk GetChunkInPosition(Int3 position)
    {
        Int3 chunkPos = new((int)MathF.Floor(position.x / 16f), (int)MathF.Floor(position.y / 16f), (int)MathF.Floor(position.z / 16f));

        return GetChunk(chunkPos);
    }
    public static Chunk GetChunk(Int3 chunkPos)
    {
        _chunks.TryGetValue(chunkPos, out Chunk? value);

        /*
        if (!_chunks.TryGetValue(chunkPos, out Chunk? value))
        {
            // Revert later
            //CreateChunkObject(chunkPos);
            return null;
        }*/

        return value;
    }
    public static WorldBlockData GetBlockAtPosition(Int3 position)
    {
        var chunk = GetChunkInPosition(position);

        if (!chunk) return WorldBlockData.AIR;

        var localPosition = chunk.WorldPositionToLocal(position);

        return chunk.GetBlockAtLocalPosition(localPosition);
    }
    public static Int3 Double3ToBlockPos(Double3 position) => (Int3)new Double3(
                position.x - Maths.Mod(position.x, 1),
                position.y - Maths.Mod(position.y, 1),
                position.z - Maths.Mod(position.z, 1));

    public static BlockRaycastHit Raycast(Double3 raycastPosition, Double3 raycastDirection, double maxRayDistance)
    {
        raycastDirection = raycastDirection.Normalized();
        double distanceTraveled = 0;

        BlockDirection blockDirectionHit = BlockDirection.SELF;
        var positionMarched = raycastPosition;

        /* Possible Optimization
        List<BlockDirection> possibleMarchingDirections = new();

        #region Populate possible marching directions

        if (raycastDirection.x > 0)
        {
            possibleMarchingDirections.Add(BlockDirection.WEST);
        }
        else if (raycastDirection.x < 0)
        {
            possibleMarchingDirections.Add(BlockDirection.EAST);
        }

        if (raycastDirection.y > 0)
        {
            possibleMarchingDirections.Add(BlockDirection.UP);
        }
        else if (raycastDirection.y < 0)
        {
            possibleMarchingDirections.Add(BlockDirection.DOWN);
        }

        if(raycastDirection.z > 0)
        {
            possibleMarchingDirections.Add(BlockDirection.NORTH);
        }
        else if(raycastDirection.z < 0)
        {
            possibleMarchingDirections.Add(BlockDirection.SOUTH);
        }


        #endregion
        */

        Int3 blockPos = Double3ToBlockPos(raycastPosition);

        WorldBlockData currentBlock = GetBlockAtPosition(blockPos);

        while (currentBlock.block_id == "minecraft:air")
        {
            // Point Marched Unit Square
            var c = positionMarched - (Double3)blockPos;

            // Unit Square Points
            Double3 v1 = new(0, 0, 0); // Down - North - West
            Double3 v2 = new(1, 0, 0); // Down - North - East
            Double3 v3 = new(1, 0, 1); // Down - South - East
            Double3 v4 = new(0, 0, 1); // Down - South - West
            Double3 v5 = new(0, 1, 1); // Up   - South - West
            Double3 v6 = new(1, 1, 1); // Up   - South - East
            Double3 v7 = new(1, 1, 0); // Up   - North - East
            Double3 v8 = new(0, 1, 0); // Up   - North - West

            // Edge Normals
            Double3 n1 = Double3.CrossProduct(v1 - c, v2 - c); // Down  - South
            Double3 n2 = Double3.CrossProduct(v2 - c, v3 - c); // Down  - East
            Double3 n3 = Double3.CrossProduct(v3 - c, v4 - c); // Down  - North
            Double3 n4 = Double3.CrossProduct(v4 - c, v1 - c); // Down  - West

            Double3 n5 = Double3.CrossProduct(v4 - c, v5 - c); // South - West
            Double3 n6 = Double3.CrossProduct(v3 - c, v6 - c); // South - East
            Double3 n7 = Double3.CrossProduct(v2 - c, v7 - c); // North - East
            Double3 n8 = Double3.CrossProduct(v1 - c, v8 - c); // North - West

            Double3 n9 = Double3.CrossProduct(v5 - c, v6 - c); // Up    - South
            Double3 n10 = Double3.CrossProduct(v6 - c, v7 - c); // Up    - East
            Double3 n11 = Double3.CrossProduct(v7 - c, v8 - c); // Up    - North
            Double3 n12 = Double3.CrossProduct(v8 - c, v5 - c); // Up    - West

            // Dot Results
            double r1 = Double3.Dot(n1, raycastDirection);
            double r2 = Double3.Dot(n2, raycastDirection);
            double r3 = Double3.Dot(n3, raycastDirection);
            double r4 = Double3.Dot(n4, raycastDirection);
            double r5 = Double3.Dot(n5, raycastDirection);
            double r6 = Double3.Dot(n6, raycastDirection);

            double r7  = Double3.Dot(n7,  raycastDirection);
            double r8  = Double3.Dot(n8,  raycastDirection);
            double r9  = Double3.Dot(n9,  raycastDirection);
            double r10 = Double3.Dot(n10, raycastDirection);
            double r11 = Double3.Dot(n11, raycastDirection);
            double r12 = Double3.Dot(n12, raycastDirection);

            /*
            Program.Log($"""
                Results:
                    r1  = '{0f.CompareTo(r1)}'
                    r2  = '{0f.CompareTo(r2)}'
                    r3  = '{0f.CompareTo(r3)}'
                    r4  = '{0f.CompareTo(r4)}'
                    r5  = '{0f.CompareTo(r5)}'
                    r6  = '{0f.CompareTo(r6)}'
                """);
            //*/

            List<BlockDirection> nextBlockDirections = new();

            float s = 0.00001f;

            // r1  - r2  - r3  - r4  - r5  - r6  - r7  - r8  - r9  - r10 - r11 - r12

            if (r1 >= -s && r2 >= -s && r3 >= -s && r4 >= -s)
            {
                nextBlockDirections.Add(BlockDirection.DOWN);
            }
            else if (r9 >= -s && r10 >= -s && r11 >= -s && r12 >= -s)
            {
                nextBlockDirections.Add(BlockDirection.UP);
            }

            // r1  - r2  - r3  - r4  - r5  - r6  - r7  - r8  - r9  - r10 - r11 - r12

            if (r4 <= s && r5 >= -s && r8 <= s && r12 <= s)
            {
                nextBlockDirections.Add(BlockDirection.WEST);
            }
            else if (r2 <= s && r6 <= s && r7 >= -s && r10 <= s)
            {
                nextBlockDirections.Add(BlockDirection.EAST);
            }

            // r1  - r2  - r3  - r4  - r5  - r6  - r7  - r8  - r9  - r10 - r11 - r12

            if (r3 <= s && r5 <= s && r6 >= -s && r9 <= s)
            {
                nextBlockDirections.Add(BlockDirection.SOUTH);
            }
            else if (r1 <= s && r7 <= s && r8 >= -s && r11 <= s)
            {
                nextBlockDirections.Add(BlockDirection.NORTH);
            }


            /* DEBUGGING
            var Values = (r1 < 0 ? "Neg" : "Pos") + " - " +
                         (r2 < 0 ? "Neg" : "Pos") + " - " +
                         (r3 < 0 ? "Neg" : "Pos") + " - " +
                         (r4 < 0 ? "Neg" : "Pos") + " - " +
                         (r5 < 0 ? "Neg" : "Pos") + " - " +
                         (r6 < 0 ? "Neg" : "Pos") + " - " +
                         (r7 < 0 ? "Neg" : "Pos") + " - " +
                         (r8 < 0 ? "Neg" : "Pos") + " - " +
                         (r9 < 0 ? "Neg" : "Pos") + " - " +
                         (r10 < 0 ? "Neg" : "Pos") + " - " +
                         (r11 < 0 ? "Neg" : "Pos") + " - " +
                         (r12 < 0 ? "Neg" : "Pos");

            if (nextBlockDirections.Count == 0)
                Program.Log("NULL!");
            else
            {
                string message = "";

                foreach (var item in nextBlockDirections)
                {
                    message += item + " - ";
                }
                Program.Log(message);
            }
            */

            if (nextBlockDirections.Count == 0)
            {
                Engine.LogError("No possible next directions! This should be impossible!");
                break;
            }

            BlockDirection nextBlockDirection = BlockDirection.NORTH;

            double closest = -10;

            // TODO: If possible marching directions is implemented, this may be redundant
            foreach (var direction in nextBlockDirections)
            {
                double dot = Double3.Dot(raycastDirection, new BlockFace(direction).GetNormal());

                if(dot > closest)
                {
                    closest = dot;
                    nextBlockDirection = direction;
                }
            }

            Double3 pointToMarchTo = Float3.zero;

            switch (nextBlockDirection)
            {
                case BlockDirection.UP:
                    pointToMarchTo = Maths.PlanePointIntersection(c, raycastDirection, Axis.Yaxis, 1) + blockPos;
                    blockPos += new Int3(0, 1, 0);
                    break;
                case BlockDirection.DOWN:
                    pointToMarchTo = Maths.PlanePointIntersection(c, raycastDirection, Axis.Yaxis) + blockPos;
                    blockPos += new Int3(0, -1, 0);
                    break;
                case BlockDirection.SOUTH:
                    pointToMarchTo = Maths.PlanePointIntersection(c, raycastDirection, Axis.Zaxis, 1) + blockPos;
                    blockPos += new Int3(0, 0, 1);
                    break;
                case BlockDirection.NORTH:
                    pointToMarchTo = Maths.PlanePointIntersection(c, raycastDirection, Axis.Zaxis) + blockPos;
                    blockPos += new Int3(0, 0, -1);
                    break;
                case BlockDirection.EAST:
                    pointToMarchTo = Maths.PlanePointIntersection(c, raycastDirection, Axis.Xaxis, 1) + blockPos;
                    blockPos += new Int3(1, 0, 0);
                    break;
                case BlockDirection.WEST:
                    pointToMarchTo = Maths.PlanePointIntersection(c, raycastDirection, Axis.Xaxis) + blockPos;
                    blockPos += new Int3(-1, 0, 0);
                    break;
            }

            double distance = (pointToMarchTo - positionMarched).Length;

            distanceTraveled += distance;

            if (distanceTraveled > maxRayDistance)
            {
                positionMarched = raycastPosition + raycastDirection * maxRayDistance;
                break;
            }

            positionMarched = pointToMarchTo;

            currentBlock = GetBlockAtPosition(blockPos);
            blockDirectionHit = BlockFace.Opposite(nextBlockDirection);
        }

        return new BlockRaycastHit(currentBlock, blockPos, new BlockFace(blockDirectionHit), positionMarched);
    }

    static readonly Int3[] adjacentBlocksSquared = [
        new(-1,-1,-1),
        new(0,-1,-1),
        new(1,-1,-1),
        new(-1,0,-1),
        new(0,0,-1),
        new(1,0,-1),
        new(-1,1,-1),
        new(0,1,-1),
        new(1,1,-1),
        new(-1,-1,0),
        new(0,-1,0),
        new(1,-1,0),
        new(-1,0,0),
        new(1,0,0),
        new(-1,1,0),
        new(0,1,0),
        new(1,1,0),
        new(-1,-1,1),
        new(0,-1,1),
        new(1,-1,1),
        new(-1,0,1),
        new(0,0,1),
        new(1,0,1),
        new(-1,1,1),
        new(0,1,1),
        new(1,1,1),
        ];
    public static bool SetBlock(Int3 position, string block_id, bool regenerate = true)
    {
        var chunk = GetChunkInPosition(position);

        if(chunk == null)
        {
            Engine.Log("Chunk not loaded yet!");
            return false;
        }

        chunk.SetBlockAtLocalPosition(chunk.WorldPositionToLocal(position), new(block_id));

        List<Chunk> chunks = [chunk];

        foreach (var adjacentBlockPos in adjacentBlocksSquared)
        {
            Chunk otherChunk = GetChunkInPosition(position + adjacentBlockPos);

            if (otherChunk == null) continue;

            if(!chunks.Contains(otherChunk))
                chunks.Add(otherChunk);
        }

        if(regenerate)
            CreateChunkGenerationJob(chunks);

        return true;
    }

    private static (Int3 min, Int3 max) PointsToMinMax(Int3 p1, Int3 p2)
    {
        int minX = p1.x;
        int maxX = p2.x;

        if (p1.x > p2.x)
        {
            minX = p2.x;
            maxX = p1.x;
        }

        int minY = p1.y;
        int maxY = p2.y;

        if (p1.y > p2.y)
        {
            minY = p2.y;
            maxY = p1.y;
        }

        int minZ = p1.z;
        int maxZ = p2.z;

        if (p1.z > p2.z)
        {
            minZ = p2.z;
            maxZ = p1.z;
        }

        return (new(minX, minY, minZ), new(maxX, maxY, maxZ));
    }
    public static void FillBlocks(Int3 p1, Int3 p2, string block_id, bool regenerateMesh = true)
    {
        var (min, max) = PointsToMinMax(p1, p2);

        List<Chunk> chunks = new();

        for (int y = min.y; y <= max.y; y++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    var chunk = GetChunkInPosition(new(x, y, z));

                    if (chunk == null) continue;

                    if (!chunks.Contains(chunk))
                    {
                        chunks.Add(chunk);
                    }

                    chunk.SetBlockAtLocalPosition(chunk.WorldPositionToLocal(new(x, y, z)), new(block_id));
                }
            }
        }

        if(regenerateMesh)
            CreateChunkGenerationJob(chunks);
    }

    public static void FillCheckerPattern(Int3 p1, Int3 p2, string block_id1, string block_id2, bool regenerateMesh = true)
    {
        var (min, max) = PointsToMinMax(p1, p2);

        List<Chunk> chunks = new();

        bool flip = true;

        for (int y = min.y; y <= max.y; y++)
        {
            for (int z = min.z; z <= max.z; z++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    var chunk = GetChunkInPosition(new(x, y, z));
                    if (chunk == null) continue;

                    if (!chunks.Contains(chunk)) chunks.Add(chunk);

                    chunk.SetBlockAtLocalPosition(chunk.WorldPositionToLocal(new(x, y, z)), new(flip ? block_id1 : block_id2));
                    flip = !flip;
                }
            }
        }

        if (regenerateMesh)
            CreateChunkGenerationJob(chunks);
    }

    [Command("setblock", "[#x, #y, #z, 'block_id']")]
    public static void SetBlock(int x, int y, int z, string block_id)
    {
        if (!block_id.Contains(':')) block_id = "minecraft:" + block_id;

        SetBlock(new(x, y, z), block_id);

        Engine.Log("Set block to: " + block_id);
    }

    [Command("fill", "[#x1, #y1, #z1, #x2, #y2, #z2, 'block_id']")]
    public static void FillBlocks(int x1, int y1, int z1, int x2, int y2, int z2, string block_id)
    {
        if (!block_id.Contains(':')) block_id = "minecraft:" + block_id;

        FillBlocks(new Int3(x1, y1, z1), new Int3(x2, y2, z2), block_id);
    }

    [Command("clearchunk", "[#x, #y, #z]")]
    public static void ClearChunk(int x, int y, int z)
    {
        var chunk = GetChunkInPosition(new(x, y, z));

        if (chunk == null)
        {
            Engine.LogError("Chunk not loaded");
            return;
        }

        chunk.ClearChunk();
        CreateChunkGenerationJob([chunk]);
    }

    [Command("refreshchunk", "[#x, #y, #z] Force Refresh Chunk")]
    public static void ForceRefresh(int x, int y, int z)
    {
        var chunk = GetChunkInPosition(new(x, y, z));

        if (chunk == null)
        {
            Engine.LogError("Chunk not loaded");
            return;
        }

        CreateChunkGenerationJob([chunk]);
    }
}