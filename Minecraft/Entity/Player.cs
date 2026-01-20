using BoboEngine;
using BoboEngine.Input;
using BoboEngine.Shaders;
using ConsoleCommand;
using GLFW;
using Minecraft.Inputs;
using Minecraft.World;
using Cursor = BoboEngine.Input.Cursor;

namespace Minecraft.Entites;

public class Player : LivingEntity
{
    public static Player Instance { get; private set; }

    // Init
    public float sensitivity = 1f;
    public float fov = 100;

    // Runtime Variables

    private int sprintTriggerTime;
    private int jumpTriggerTime;
    public bool crouching;
    public bool flying;
    public bool mayFly = true;

    // For item visual
    public float yBob;
    public float xBob;
    public float yBobOld;
    public float xBobOld;

    public Mesh boxSelectionMesh;
    public BlockRaycastHit lastHit;
    public string blockSelected = "minecraft:dirt";

    Mesh colliderMesh;

    Input.MouseMapping keyUse;
    Input.MouseMapping keyAttack;
    Input.MouseMapping keyPickItem;

    #region Initial Methods
    public override void Start()
    {
        Instance = this;

        flyingSpeed = 0.05f;
        movementSpeed = 0.1f;
        walkingSpeed = 0.1f;
        sneakingSpeed = 0.3f;
        gravity = 0.08;
        maxUpStep = 0.6f;
        jumpPower = 0.42f;

        boundingBoxSize = new(0.6, 1.8, 0.6);

        keyUse = Input.GetMouseMapping(MouseButton.Right);
        keyAttack = Input.GetMouseMapping(MouseButton.Left);
        keyPickItem = Input.GetMouseMapping(MouseButton.Middle);

        base.Start();

        colliderMesh = CreateDebugMesh((Float3)boundingBoxSize);
        colliderMesh.gameObject.enabled = debugCollisions;

        boxSelectionMesh = new GameObject("BoxSelection").AddComponent<Mesh>();

        boxSelectionMesh.LoadObjFile(Engine.GetLocalModelPath("CubeOutline"));

        string shaderID = "boxSelect";

        ShaderManager.EnsureShader(shaderID, "Shader/boxSelectionShader.vert", "Shader/boxSelectionShader.frag");

        boxSelectionMesh.material = new Material(shaderID, cullBackFaces: false, renderOrder: 1);
        boxSelectionMesh.material.SetVec4("Color", new(0, 0, 0, 0.4f));

        Cursor.mode = CursorMode.Disabled;
    }
    public override void Update()
    {
        base.Update();

        Camera.main.fov = GetFov(Camera.main, Minecraft.partialTickTime, true);

        rotation += GetRotationInput();
        UpdateRaycast();

        var cam = Camera.main.transform;

        cam.position = (Float3)GetEyePos();
        cam.rotation = new(rotation.x, rotation.y);

        colliderMesh.transform.position = transform.position;
    }
    public void UpdateRaycast()
    {
        lastHit = WorldChunkManager.Raycast(GetEyePos(), GetLookVector(), 5);

        if (lastHit)
        {
            boxSelectionMesh.gameObject.enabled = true;
            boxSelectionMesh.transform.position = lastHit.blockPosition + new Float3(0.5f, 0.5f, 0.5f);
            boxSelectionMesh.material.SetVec2("ScreenSize", WindowManager.WindowSize); // TODO: Maybe only fire when the screen size actually changes
        }
        else
        {
            boxSelectionMesh.gameObject.enabled = false;
        }
    }

    protected override void OnTick()
    {
        TickFov();

        base.OnTick();

        UpdateInput();
    }

    int placeDelay;
    int destroyDelay;
    private void UpdateInput()
    {
        if (placeDelay > 0) placeDelay--;
        if (destroyDelay > 0) destroyDelay--;


        // Buffer
        while (keyAttack.ConsumeClick())
        {
            DestroyBlock();
        }

        while (keyUse.ConsumeClick())
        {
            PlaceBlock();
        }

        while (keyPickItem.ConsumeClick())
        {
            PickBlock();
        }

        // Input Holding
        if(keyUse.isDown && placeDelay == 0)
        {
            PlaceBlock();
        }
        else if (!keyUse.isDown)
        {
            placeDelay = 0;
        }

        if (keyAttack.isDown && destroyDelay == 0)
        {
            DestroyBlock();
        }
        else if (!keyAttack.isDown)
        {
            destroyDelay = 0;
        }
    }

    float oldFovModifier;
    float fovModifier;
    private void TickFov()
    {
        float targetFovModifier = 1;

        bool firstPerson = true;
        float effectScale = 1f; // Default FOV
        targetFovModifier = GetFieldOfViewModifier(firstPerson, effectScale);

        oldFovModifier = fovModifier;
        fovModifier += (targetFovModifier - fovModifier) * 0.5F;
        fovModifier = Maths.Clamp(fovModifier, 0.1F, 1.5F);
    }
    public float GetFieldOfViewModifier(bool firstPerson, float effectScale)
    {
        float modifier = 1.0F;
        if (flying)
        {
            modifier *= 1.1F;
        }

        if (walkingSpeed != 0.0F)
        {
            float speedFactor = speed / walkingSpeed;
            modifier *= (speedFactor + 1.0F) / 2.0F;
        }

        return Maths.Lerp(1.0F, modifier, effectScale);
    }
    public override void AiStep()
    {
        if (sprintTriggerTime > 0) sprintTriggerTime--;

        bool wasShiftPressed = shiftPressed;
        bool wasSpacePressed = spacePressed;
        bool hadForwardImpulse = HasForwardImpulse();

        crouching = !flying && !isPassenger() && CanPlayerFitWithinBlocksAndEntitiesWhen(Pose.CROUCHING) && (InputSystem.GetKey(Keys.LeftShift) || !CanPlayerFitWithinBlocksAndEntitiesWhen(Pose.STANDING));

        if(crouching)
        {
            SetEyeHeight(1.27);
            boundingBoxSize.y = 1.5;
        }
        else
        {
            SetEyeHeight(1.625);
            boundingBoxSize.y = 1.8;
        }


        TickInput();

        if (wasShiftPressed || moveVector.y < 0)
        {
            sprintTriggerTime = 0;
        }

        if (CanStartSprinting())
        {
            if (!hadForwardImpulse)
            {
                if (sprintTriggerTime > 0)
                {
                    sprinting = true;
                }
                else
                {
                    sprintTriggerTime = 7; // Default sprint tick window
                }
            }

            if (ctrlPressed)
            {
                sprinting = true;
            }
        }

        if (sprinting)
        {
            if (ShouldStopRunSprinting())
            {
                sprinting = false;
            }
        }

        if (mayFly)
        {
            /*
            if (this.minecraft.gameMode.isSpectator())
            {
                if (!abilities.flying)
                {
                    abilities.flying = true;
                    justToggledCreativeFlight = true;
                    this.onUpdateAbilities();
                }
            }
            else */
            if (!wasSpacePressed && spacePressed)
            {
                if (jumpTriggerTime == 0)
                {
                    jumpTriggerTime = 7;
                }
                else
                {
                    flying = !flying;
                    if (flying && onGround)
                    {
                        JumpFromGround();
                    }

                    jumpTriggerTime = 0;
                }
            }
        }

        if (flying)
        {
            int inputYa = 0;
            if (shiftPressed)
            {
                inputYa--;
            }

            if (spacePressed)
            {
                inputYa++;
            }

            if (inputYa != 0)
            {
                deltaMovement.y += inputYa * flyingSpeed * 3.0F;
            }
        }

        if (jumpTriggerTime > 0) jumpTriggerTime--;

        base.AiStep();

        if (onGround && flying)
        {
            flying = false;
        }
    }
    #endregion

    #region Movement Methods
    protected bool CanPlayerFitWithinBlocksAndEntitiesWhen(Pose newPose)
    {
        BoundingBox bb = GetBoundingBox(newPose).Deflate(1.0E-7);

        return WorldChunkManager.NoCollision(this, bb);
    }

    public override void Travel(Double3 input)
    {
        if (isPassenger())
        {
            base.Travel(input);
        }
        else
        {
            /*
            if (this.isSwimming())
            {
                double lookAngleY = this.getLookAngle().y;
                double multiplier = lookAngleY < -0.2 ? 0.085 : 0.06;
                if (lookAngleY <= 0.0 || this.jumping || !this.level().getFluidState(BlockPos.containing(this.getX(), this.getY() + 1.0 - 0.1, this.getZ())).isEmpty())
                {
                    Vec3 movement = this.getDeltaMovement();
                    this.setDeltaMovement(movement.add(0.0, (lookAngleY - movement.y) * multiplier, 0.0));
                }
            }
            */

            if (flying)
            {
                double originalMovementY = deltaMovement.y;
                base.Travel(input);
                deltaMovement.y = originalMovementY * 0.6;
            }
            else
            {
                base.Travel(input);
            }
        }
    }
    public override void ApplyInput()
    {
        Float2 modifiedInput = ModifyInput(moveVector);

        input.x = modifiedInput.x;
        input.z = modifiedInput.y;
        jumping = InputSystem.GetKey(Keys.Space);

        yBobOld = yBob;
        xBobOld = xBob;
        xBob = xBob + (rotation.x - xBob) * 0.5F;
        yBob = yBob + (rotation.y - yBob) * 0.5F;
    }

    protected override Double3 MaybeBackOffFromEdge(Double3 delta)
    {
        float maxDownStep = MaxUpStep();
        if (!flying && !(delta.y > 0.0) && crouching && IsAboveGround(maxDownStep))
        {
            double deltaX = delta.x;
            double deltaZ = delta.z;
            double step = 0.05;
            double stepX = Math.Sign(deltaX) * step;

            double stepZ;
            for (stepZ = Math.Sign(deltaZ) * step; deltaX != 0.0 && CanFallAtLeast(deltaX, 0.0, maxDownStep); deltaX -= stepX)
            {
                if (Math.Abs(deltaX) <= step)
                {
                    deltaX = 0.0;
                    break;
                }
            }

            while (deltaZ != 0.0 && CanFallAtLeast(0.0, deltaZ, maxDownStep))
            {
                if (Math.Abs(deltaZ) <= step)
                {
                    deltaZ = 0.0;
                    break;
                }

                deltaZ -= stepZ;
            }

            while (deltaX != 0.0 && deltaZ != 0.0 && CanFallAtLeast(deltaX, deltaZ, maxDownStep))
            {
                if (Math.Abs(deltaX) <= step)
                {
                    deltaX = 0.0;
                }
                else
                {
                    deltaX -= stepX;
                }

                if (Math.Abs(deltaZ) <= step)
                {
                    deltaZ = 0.0;
                }
                else
                {
                    deltaZ -= stepZ;
                }
            }

            return new Double3(deltaX, delta.y, deltaZ);
        }
        else
        {
            return delta;
        }
    }
    private bool IsAboveGround(float maxDownStep)
    {
        return onGround || fallDistance < maxDownStep && !CanFallAtLeast(0.0, 0.0, maxDownStep - this.fallDistance);
    }
    private bool CanFallAtLeast(double deltaX, double deltaZ, double minHeight)
    {
        // floating point offset
        var fpo = 1.0E-7;

        var boundingBox = GetBoundingBox();
        return WorldChunkManager.NoCollision(
                this,
                new BoundingBox(
                    boundingBox.minX + fpo + deltaX,
                    boundingBox.minY - minHeight - fpo,
                    boundingBox.minZ + fpo + deltaZ,
                    boundingBox.maxX - fpo + deltaX,
                    boundingBox.minY,
                    boundingBox.maxZ - fpo + deltaZ
                )
            );
    }
    private Float2 ModifyInput(Float2 input)
    {
        if (input.LengthSquared() == 0.0F)
        {
            return input;
        }
        else
        {
            Float2 newInput = input * 0.98f;

            if (crouching)
                newInput *= sneakingSpeed;

            return newInput; //ModifyInputSpeedForSquareMovement(newInput);
        }
    }
    private static Float2 ModifyInputSpeedForSquareMovement(Float2 input)
    {
        float length = input.Length;
        if (length <= 0.0F)
        {
            return input;
        }
        else
        {
            Float2 direction = input * (1 / length);
            float distanceToUnitSquare = DistanceToUnitSquare(direction);
            float modifiedLength = Math.Min(length * distanceToUnitSquare, 1.0F);
            return direction * modifiedLength;
        }
    }

    #endregion

    #region Private Methods

    // Get
    private bool CanStartSprinting()
    {
        return !sprinting && HasForwardImpulse() && !crouching;
    }
    private bool ShouldStopRunSprinting()
    {
        return !HasForwardImpulse() || horizontalCollision && !minorHorizontalCollision;
    }
    private float GetFov(Camera camera, float partialTicks, bool applyEffects)
    {
        float fov = this.fov;

        if (applyEffects)
        {
            fov *= Maths.Lerp(oldFovModifier, fovModifier, partialTicks);
        }

        return fov;
    }

    // Static
    private static float DistanceToUnitSquare(Float2 direction)
    {
        float directionX = Math.Abs(direction.x);
        float directionY = Math.Abs(direction.y);
        float tan = directionY > directionX ? directionX / directionY : directionY / directionX;
        return (float)Math.Sqrt(1.0F + (tan * tan));
    }

    #endregion

    #region Input

    Float2 moveVector;
    bool shiftPressed;
    bool spacePressed;
    bool ctrlPressed;
    private void TickInput()
    {
        moveVector = Float2.zero;

        var w = InputSystem.GetKey(Keys.W);
        var a = InputSystem.GetKey(Keys.A);
        var s = InputSystem.GetKey(Keys.S);
        var d = InputSystem.GetKey(Keys.D);

        if (w)
        {
            moveVector += Float2.yAxis;
        }
        if (s)
        {
            moveVector -= Float2.yAxis;
        }
        if (a)
        {
            moveVector += Float2.xAxis;
        }
        if (d)
        {
            moveVector -= Float2.xAxis;
        }

        shiftPressed = InputSystem.GetKey(Keys.LeftShift);
        spacePressed = InputSystem.GetKey(Keys.Space);
        ctrlPressed = InputSystem.GetKey(Keys.LeftControl);
    }
    private Float2 GetRotationInput() => new Float2(Cursor.delta.y, Cursor.delta.x) * (0.14f * sensitivity);

    private void PlaceBlock()
    {
        if (!lastHit) return;

        Int3 blockToChange = (Int3)(lastHit.blockPosition + lastHit.blockFace.GetNormal());

        if(WorldChunkManager.IsBlockInBoundingBox(blockToChange, blockSelected, GetBoundingBox())) return;

        placeDelay = 4;

        //Program.Log("Place Block!");

        WorldChunkManager.SetBlock(blockToChange, blockSelected);
        UpdateRaycast();
    }
    private void DestroyBlock()
    {
        if (!lastHit) return;

        Int3 blockToChange = lastHit.blockPosition;

        if (WorldChunkManager.GetBlockAtPosition(blockToChange).block_id == "minecraft:bedrock") return;

        destroyDelay = 5;

        //Program.Log("Destroy Block!");

        WorldChunkManager.SetBlock(lastHit.blockPosition, "minecraft:air");
        UpdateRaycast();
    }
    private static void PickBlock()
    {
        if (!Instance.lastHit) return;

        if (Instance.lastHit.blockHit.block_id == "minecraft:bedrock") return;

        Instance.blockSelected = Instance.lastHit.blockHit.block_id;
        Engine.Log($"Selected: '{Instance.blockSelected}'");
    }

    #endregion

    #region Public Methods

    // Get
    public bool HasForwardImpulse()
    {
        return moveVector.y > 1.0E-5f;
    }
    public override float GetFlyingSpeed()
    {
        if (flying)
            return sprinting ? flyingSpeed * 2.0F : flyingSpeed;
        else
            return sprinting ? 0.025999999F : 0.02F;
    }


    #endregion

    // Commands
    [Command("block", "['blockID'] Sets the block to place")]
    public static void ChangeBlock(string type)
    {
        if (!type.Contains(':')) type = "minecraft:" + type;

        Instance.blockSelected = type;
        Engine.Log($"Selected: '{type}'");
    }

    [Command("mayFly", "[bool] Changes the mayFly property")]
    public static void MayFly(bool value)
    {
        Instance.mayFly = value;

        Engine.Log("mayFly is now set to = " + value);
    }
    [Command("fov", "[#value] Sets the visual fov")]
    public static void SetFOV(int value)
    {
        Instance.fov = Maths.Clamp(value, 30, 120);
        Engine.LogMessage($"Fov set to '{Instance.fov}'");
    }
}

public enum Pose
{
    STANDING,
    CROUCHING
}