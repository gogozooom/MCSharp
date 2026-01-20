using BoboEngine;
using BoboEngine.Shaders;
using Minecraft.Collision;
using Minecraft.World;

namespace Minecraft.Entites;

public abstract class Entity : ObjectBehavior
{
    // Init
    public Double3 boundingBoxSize;
    private double eyeHeight;
    public double GetEyeHeight() => Maths.Lerp(oldEyeHeight, eyeHeight, Minecraft.partialTickTime);
    public void SetEyeHeight(double v) { oldEyeHeight = eyeHeight; eyeHeight = v; }
    private double oldEyeHeight;
    public bool noGravity;

    public bool debugCollisions = false;

    // Placement
    public Double3 deltaMovement;

    public Float3 position;

    public Float2 rotation { get => _rotation; set => SetRotation(value); }
    private Float2 _rotation;
    private void SetRotation(Float2 input) => _rotation = new Float2(Math.Clamp(input.x, -90f, 90f), input.y);

    // Runtime Paramaters
    public double fallDistance;
    public bool onGround { get; private set; }
    public bool horizontalCollision { get; private set; }
    public bool verticalCollision { get; private set; }
    public bool verticalCollisionBelow { get; private set; }
    public bool minorHorizontalCollision { get; private set; }
    private bool onGroundNoBlocks;
    public Int3? mainSupportingBlockPos = null;

    #region Overridable Paramaters
    protected virtual bool IsHorizontalCollisionMinor(Double3 movement) => false;
    protected virtual float GetBlockSpeedFactor() => 1;
    public virtual double GetGravity()
    {
        return noGravity ? 0.0 : GetDefaultGravity();
    }
    protected virtual double GetDefaultGravity()
    {
        return 0.0;
    }
    public virtual float MaxUpStep() => 0.0f;
    #endregion

    #region Initial Methods
    public override void Start()
    {
        base.Start();

        Minecraft.onTick += OnTick;
    }
    public override void Update()
    {
        base.Update();
        transform.position = GetLerpedPosition(Minecraft.partialTickTime);
    }
    private Float3 GetLerpedPosition(float partialTickTime)
    {
        var movement = movementThisTick.Last?.Value;

        if (movement == null || movement.tick != Minecraft.tick) return position;

        return Float3.Lerp((Float3)movement.from, (Float3)movement.to, partialTickTime);
    }

    protected virtual void OnTick()
    {
        // portal and fire behavior here
    }
    #endregion

    #region Collision Methods

    private Double3 Collide(Double3 movement)
    {
        BoundingBox aabb = GetBoundingBox();
        List<VoxelShape> entityColliders = WorldChunkManager.GetEntityCollisions(this, aabb.ExpandTowards(movement));
        Double3 movementStep = movement.LengthSquared() == 0.0 ? movement : CollideBoundingBox(this, movement, aabb, entityColliders);
        bool xCollision = movement.x != movementStep.x;
        bool yCollision = movement.y != movementStep.y;
        bool zCollision = movement.z != movementStep.z;
        bool onGroundAfterCollision = yCollision && movement.y < 0.0;

        if (MaxUpStep() > 0.0F && (onGroundAfterCollision || onGround) && (xCollision || zCollision))
        {
            BoundingBox groundedAABB = onGroundAfterCollision ? aabb.Move(0.0, movementStep.y, 0.0) : aabb;
            BoundingBox stepUpAABB = groundedAABB.ExpandTowards(movement.x, MaxUpStep(), movement.z);
            if (!onGroundAfterCollision)
            {
                stepUpAABB = stepUpAABB.ExpandTowards(0.0, -1.0E-5F, 0.0);
            }

            List<VoxelShape> colliders = CollectColliders(this, entityColliders, stepUpAABB);
            float stepHeightToSkip = (float)movementStep.y;
            float[] candidateStepUpHeights = CollectCandidateStepUpHeights(groundedAABB, colliders, MaxUpStep(), stepHeightToSkip);

            foreach (float candidateStepUpHeight in candidateStepUpHeights)
            {
                Double3 stepFromGround = CollideWithShapes(new Double3(movement.x, candidateStepUpHeight, movement.z), groundedAABB, colliders);
                if (stepFromGround.HorizontalDistanceSqr() > movementStep.HorizontalDistanceSqr())
                {
                    double distanceToGround = aabb.minY - groundedAABB.minY;
                    return stepFromGround - new Double3(0.0, distanceToGround, 0.0);
                }
            }
        }

        return movementStep;
    }
    public static Double3 CollideBoundingBox(Entity source, Double3 movement, BoundingBox boundingBox, List<VoxelShape> entityColliders)
    {
        List<VoxelShape> colliders = CollectColliders(source, entityColliders, boundingBox.ExpandTowards(movement));

        source.ResetDebugMeshes();

        if (source.debugCollisions)
        {
            int i = 0;

            foreach (var collider in colliders)
            {
                foreach (var shape in collider.shapes)
                {
                    var mesh = source.GetInactiveDebugMesh(colliders.Count, i);

                    mesh.transform.scale = (Float3)(shape.localFrom - shape.localTo);
                    mesh.transform.position = collider.position + (Float3)shape.localFrom;
                }
                i++;
            }
        }

        return CollideWithShapes(movement, boundingBox, colliders);
    }
    private static Double3 CollideWithShapes(Double3 movement, BoundingBox boundingBox, List<VoxelShape> shapes)
    {
        if (shapes.Count == 0) return movement;

        Double3 resolvedMovement = Double3.zero;

        foreach (Axis axis in BlockFace.AxisStepOrder(movement))
        {
            double axisMovement = movement.GetAxis(axis);
            if (axisMovement != 0.0)
            {
                double collision = Shapes.Collide(axis, boundingBox.Move(resolvedMovement), shapes, axisMovement);

                resolvedMovement = resolvedMovement.With(axis, collision);
            }
        }

        return resolvedMovement;
    }
    private static List<VoxelShape> CollectColliders(Entity source, List<VoxelShape> entityColliders, BoundingBox boundingBox)
    {
        List<VoxelShape> colliders = new();
        if (entityColliders.Count != 0)
        {
            colliders.AddRange(entityColliders);
        }

        colliders.AddRange(WorldChunkManager.GetBlockCollisions(source, boundingBox));
        return colliders;
    }
    private static float[] CollectCandidateStepUpHeights(BoundingBox boundingBox, List<VoxelShape> colliders, float maxStepHeight, float stepHeightToSkip)
    {
        List<float> candidates = new();

        foreach (VoxelShape collider in colliders)
        {
            foreach (double coord in collider.GetCoords(Axis.Yaxis))
            {
                float relativeCoord = (float)(coord - boundingBox.minY);

                if (!(relativeCoord < 0.0F) && relativeCoord != stepHeightToSkip)
                {
                    if (relativeCoord > maxStepHeight)
                    {
                        break;
                    }

                    candidates.Add(relativeCoord);
                }
            }
        }

        candidates.Sort();

        return candidates.ToArray();
    }

    #endregion

    #region Private Methods

    // Action
    protected void CheckSupportingBlock(bool onGround, Double3? movement)
    {
        if (onGround)
        {
            BoundingBox boundingBox = GetBoundingBox();
            BoundingBox testArea = new BoundingBox(boundingBox.minX, boundingBox.minY - 1.0E-6, boundingBox.minZ, boundingBox.maxX, boundingBox.minY, boundingBox.maxZ);
            var supportingBlock = WorldChunkManager.FindSupportingBlock(this, testArea);

            if (supportingBlock != null || onGroundNoBlocks)
            {
                mainSupportingBlockPos = supportingBlock;
            }
            else if (movement != null)
            {
                BoundingBox onGroundCollisionTestArea = testArea.Move(-movement.Value.x, 0.0, -movement.Value.z);
                supportingBlock = WorldChunkManager.FindSupportingBlock(this, onGroundCollisionTestArea);
                mainSupportingBlockPos = supportingBlock;
            }

            onGroundNoBlocks = supportingBlock == null;
        }
        else
        {
            onGroundNoBlocks = false;
            if (mainSupportingBlockPos != null)
            {
                mainSupportingBlockPos = null;
            }
        }
    }

    // Get
    protected static Double3 GetRelativeInput(Double3 input, float speed, float yRot)
    {
        double length = input.LengthSquared();
        if (length < 1.0E-7)
        {
            return Double3.zero;
        }
        else
        {
            Double3 movement = (length > 1.0 ? input.Normalized() : input) * speed;

            movement = BaseVectors.FromYRotation(yRot).TransformVector(movement);

            return movement;
        }
    }
    protected Int3 GetOnPos(float offset)
    {
        if (mainSupportingBlockPos != null)
        {
            Int3 getOnPos = mainSupportingBlockPos.Value;
            if (!(offset > 1.0E-5F))
            {
                return getOnPos;
            }
            else
            {
                return (!(offset <= 0.5))
                    ? getOnPos.With(Axis.Yaxis, Maths.Floor(position.y - offset))
                    : getOnPos;
            }
        }
        else
        {
            int xTruncated = Maths.Floor(position.x);
            int yTruncatedBelow = Maths.Floor(position.y - offset);
            int zTruncated = Maths.Floor(position.z);
            return new Int3(xTruncated, yTruncatedBelow, zTruncated);
        }
    }

    #endregion

    #region Records

    private readonly LinkedList<Movement> movementThisTick = new();
    public record Movement(int tick, Double3 from, Double3 to, Double3 axisDependentOriginalMovement = new());
    private void AddMovementThisTick(Movement movement)
    {
        if (movementThisTick.Count >= 100)
        {
            Movement first = movementThisTick.First(); movementThisTick.RemoveFirst();
            Movement second = movementThisTick.First(); movementThisTick.RemoveFirst();
            Movement combined = new Movement(Minecraft.tick, first.from, second.to);
            movementThisTick.AddFirst(combined);
        }

        movementThisTick.AddLast(movement);
    }
    public void RemoveLatestMovementRecording()
    {
        if (movementThisTick.Count != 0)
        {
            movementThisTick.RemoveLast();
        }
    }
    protected void ClearMovementThisTick()
    {
        movementThisTick.Clear();
    }
    #endregion

    #region Public Methods

    // Action
    public void Move(Double3 delta)
    {
        delta = MaybeBackOffFromEdge(delta);
        Double3 movement = Collide(delta);
        double movementLength = movement.LengthSquared();
        if (movementLength > 1.0E-7 || delta.LengthSquared() - movementLength < 1.0E-7)
        {
            if (fallDistance != 0.0 && movementLength >= 1.0)
            {
                double checkDistance = Math.Min(movement.Length, 8.0);
                Double3 checkTo = movement.Normalized();
                BlockRaycastHit hitResult = WorldChunkManager.Raycast(position, checkTo, checkDistance);
                //new ClipContext(this.position(), checkTo, ClipContext.Block.FALLDAMAGE_RESETTING, ClipContext.Fluid.WATER, this));
                if (hitResult)
                {
                    fallDistance = 0;
                }
            }

            Double3 pos = position;
            Double3 newPosition = pos + movement;
            AddMovementThisTick(new Movement(Minecraft.tick, pos, newPosition, delta));
            position = (Float3)newPosition;
        }

        bool xCollision = !Maths.Equal(delta.x, movement.x);
        bool zCollision = !Maths.Equal(delta.z, movement.z);
        horizontalCollision = xCollision || zCollision;
        if (Math.Abs(delta.y) > 0.0)// || this.isLocalInstanceAuthoritative())
        {
            verticalCollision = delta.y != movement.y;
            verticalCollisionBelow = verticalCollision && delta.y < 0.0;
            SetOnGroundWithMovement(verticalCollisionBelow, horizontalCollision, movement);
        }

        if (horizontalCollision)
        {
            minorHorizontalCollision = IsHorizontalCollisionMinor(movement);
        }
        else
        {
            minorHorizontalCollision = false;
        }

        Int3 effectPos = GetOnPos(0.2f);
        /*
        BlockState effectState = this.level().getBlockState(effectPos);

        this.checkFallDamage(movement.y, this.onGround(), effectState, effectPos);
        */
        if (horizontalCollision)
        {
            Double3 deltaM = deltaMovement;
            deltaMovement = new(xCollision ? 0.0 : deltaM.x, deltaM.y, zCollision ? 0.0 : deltaM.z);
        }

        //Block onBlock = WorldDataManager.GetBlockAtPosition(effectPos);
        if (delta.y != movement.y)
        {
            deltaMovement.y = 0;
            //onBlock.updateEntityMovementAfterFallOn(this.level(), this);
        }

        /*
        if (this.canSimulateMovement())
        {
        }
        if (!this.level().isClientSide() || this.isLocalInstanceAuthoritative())
        {
            Entity.MovementEmission emission = this.getMovementEmission();
            if (emission.emitsAnything() && !this.isPassenger())
            {
                this.applyMovementEmissionAndPlaySound(emission, movement, effectPos, effectState);
            }
        }
        */

        float blockSpeedFactor = GetBlockSpeedFactor();
        deltaMovement *= new Double3(blockSpeedFactor, 1, blockSpeedFactor);
    }
    protected virtual Double3 MaybeBackOffFromEdge(Double3 delta) => delta;
    public void MoveRelative(float speed, Double3 input)
    {
        Double3 delta = GetRelativeInput(input, speed, _rotation.y);
        deltaMovement += delta;
    }
    public void SetOnGroundWithMovement(bool onGround, Double3 movement)
    {
        SetOnGroundWithMovement(onGround, horizontalCollision, movement);
    }
    public void SetOnGroundWithMovement(bool onGround, bool horizontalCollision, Double3 movement)
    {
        this.onGround = onGround;
        this.horizontalCollision = horizontalCollision;
        CheckSupportingBlock(onGround, movement);
    }

    // Get
    public Double3 GetEyePos()
    {
        return new Double3(transform.position.x, transform.position.y + GetEyeHeight(), transform.position.z);
    }
    public bool isPassenger()
    {
        return false;
        //return this.getVehicle() != null;
    }
    public Float3 GetLookVector()
    {
        return BaseVectors.FromRotation(new(_rotation.x, _rotation.y)).forwardVector;
    }
    public BoundingBox GetBoundingBox()
    {
        return new(this);
    }
    public BoundingBox GetBoundingBox(Pose pose)
    {
        double height = 1.62;

        switch (pose)
        {
            case Pose.CROUCHING:
                height = 1.495;
                break;
            default:
                break;
        }

        return new(position, boundingBoxSize.With(Axis.Yaxis, height));
    }

    #endregion

    #region Debugging
    private static readonly List<Mesh> debugVisualsInactive = new();
    private static readonly List<Mesh> debugVisualsActive = new();
    private static Mesh oneFrameBoundingBox;
    public Mesh GetInactiveDebugMesh(int totalCount = 0, int colliderIndex = 0)
    {
        Mesh mesh;
        if (debugVisualsInactive.Count == 0)
        {
            mesh = CreateDebugMesh(Int3.one, false);
            debugVisualsActive.Add(mesh);
        }
        else
        {
            mesh = debugVisualsInactive[^1];
            debugVisualsInactive.Remove(mesh);
            mesh.gameObject.enabled = true;
            debugVisualsActive.Add(mesh);
        }

        mesh.material.SetVec4("Color", new Float4(0, (float)colliderIndex / totalCount, 0, 1));
        mesh.material.renderOrder = debugVisualsActive.Count + 1;
        return mesh;
    }
    public void ResetDebugMeshes()
    {
        debugVisualsInactive.AddRange(debugVisualsActive);

        foreach (var item in debugVisualsActive)
        {
            item.gameObject.enabled = false;
        }

        debugVisualsActive.Clear();
    }
    public void CreateOneFrameDebugMesh(BoundingBox bb)
    {
        if (oneFrameBoundingBox)
        {
            oneFrameBoundingBox.gameObject.Destroy();
            oneFrameBoundingBox = null;
        }

        var pos = new Double3((bb.maxX + bb.minX) / 2, bb.minY, (bb.maxZ + bb.minZ) / 2);
        var size = new Double3(bb.maxX - bb.minX, bb.maxY - bb.minY, bb.maxZ - bb.minZ);

        oneFrameBoundingBox = CreateDebugMesh((Float3)size);
        oneFrameBoundingBox.material.SetVec4("Color", new(1, 0, 0, 1));
        oneFrameBoundingBox.transform.position = (Float3)pos;
    }
    public Mesh CreateDebugMesh(Float3 size, bool center = true)
    {
        string shaderID = "boxSelect";

        ShaderManager.EnsureShader(shaderID, "Shader/boxSelectionShader.vert", "Shader/boxSelectionShader.frag");

        var debugMesh = new GameObject("DebugMesh").AddComponent<Mesh>();

        var (verticies, faces, normals) = BuildColliderMeshVisual(size, center);

        debugMesh.LoadRawData(verticies, faces, normals);

        debugMesh.material = new Material(shaderID, cullBackFaces: false, useDepth: false, renderOrder: 1);
        debugMesh.material.SetVec4("Color", new(1, 1, 1, 1));

        return debugMesh;
    }
    protected static (Float3[] verticies, FaceInfo[] faces, Float3[] normals) BuildColliderMeshVisual(Float3 size, bool center = true)
    {
        Float3[] verticies;

        if (center)
        {
            Float3 hSize = size / 2;

            verticies =
                [
                new(-hSize.x, 0,-hSize.z),
                new( hSize.x, 0,-hSize.z),
                new(-hSize.x, 0, hSize.z),
                new( hSize.x, 0, hSize.z),
                new(-hSize.x, size.y,-hSize.z),
                new( hSize.x, size.y,-hSize.z),
                new(-hSize.x, size.y, hSize.z),
                new( hSize.x, size.y, hSize.z),
            ];
        }
        else
        {
            verticies =
                [
                new(0,      0,      0),
                new(-size.x, 0,      0),
                new(0,      0,      -size.z),
                new(-size.x, 0,      -size.z),
                new(0,      -size.y, 0),
                new(-size.x, -size.y, 0),
                new(0,      -size.y, -size.z),
                new(-size.x, -size.y, -size.z),
            ];
        }
        Float3[] normals =
            [
                new Float3(1,0,0),
                new Float3(0,1,0),
                new Float3(0,0,1),
            ];
        string[] faceInfo =
            [
            "f 1/1/1 1/1/1 2/1/1 2/1/1",
            "f 3/1/1 3/1/1 4/1/1 4/1/1",
            "f 5/1/1 5/1/1 6/1/1 6/1/1",
            "f 7/1/1 7/1/1 8/1/1 8/1/1",
            "f 1/1/2 1/1/2 5/1/2 5/1/2",
            "f 2/1/2 2/1/2 6/1/2 6/1/2",
            "f 3/1/2 3/1/2 7/1/2 7/1/2",
            "f 4/1/2 4/1/2 8/1/2 8/1/2",
            "f 1/1/3 1/1/3 3/1/3 3/1/3",
            "f 2/1/3 2/1/3 4/1/3 4/1/3",
            "f 5/1/3 5/1/3 7/1/3 7/1/3",
            "f 6/1/3 6/1/3 8/1/3 8/1/3"
            ];

        List<FaceInfo> faces = new();

        foreach (var face in faceInfo)
        {
            faces.AddRange(FaceInfo.GetTriangulatedFaces(face));
        }

        return (verticies, faces.ToArray(), normals);
    }
    #endregion
}