using BoboEngine;

namespace Minecraft.Entites;

public abstract class LivingEntity : Entity
{
    // Init
    public float flyingSpeed = 0.05f;
    public float movementSpeed = 0.1f;
    public float walkingSpeed = 0.1f;
    public float sneakingSpeed = 0.3f;
    public double gravity = 0.08;
    public float maxUpStep = 0.6f;
    public float jumpPower = 0.42f;

    // Runtime Variables
    public Float3 input;

    protected bool jumping;
    private int noJumpDelay;

    public float speed;

    public bool sprinting { get => _sprinting; set => SetSprinting(value); }
    private bool _sprinting;
    private void SetSprinting(bool sprinting)
    {
        _sprinting = sprinting;

        speed = movementSpeed;
        if (sprinting)
        {
            speed *= 1.3f;
        }
    }

    #region Overwritten Paramaters
    protected override double GetDefaultGravity()
    {
        return gravity;
    }
    public override float MaxUpStep()
    {
        return maxUpStep;
    }
    #endregion

    #region Initial Methods
    public override void Start()
    {
        SetSprinting(false);
        base.Start();
    }
    protected override void OnTick()
    {
        base.OnTick();
        AiStep();
    }
    public virtual void AiStep()
    {
        if (noJumpDelay > 0) noJumpDelay--;

        deltaMovement *= 0.98;

        if (this is Player)
        {
            if (deltaMovement.HorizontalDistanceSqr() < 9.0E-6)
            {
                deltaMovement.x = 0;
                deltaMovement.z = 0;
            }
        }
        else
        {
            if (Math.Abs(deltaMovement.x) < 0.003)
            {
                deltaMovement.x = 0;
            }

            if (Math.Abs(deltaMovement.z) < 0.003)
            {
                deltaMovement.z = 0;
            }
        }

        if (Math.Abs(deltaMovement.y) < 0.003)
        {
            deltaMovement.y = 0;
        }

        if (jumping)
        {
            if (onGround && noJumpDelay == 0)
            {
                JumpFromGround();
                noJumpDelay = 10;
            }
        }
        else
        {
            noJumpDelay = 0;
        }

        ApplyInput();

        Travel(input);
    }
    #endregion

    #region Movement
    public virtual void ApplyInput()
    {
        input.x *= 0.98f;
        input.z *= 0.98f;
    }
    public void JumpFromGround()
    {
        if (!(jumpPower <= 1.0E-5F))
        {
            deltaMovement.y = Math.Max(jumpPower, deltaMovement.y);
            if (sprinting)
            {
                deltaMovement += BaseVectors.FromYRotation(rotation.y).forwardVector * 0.2f;
            }
        }
    }
    public virtual void Travel(Double3 input)
    {
        /*
        if (this.shouldTravelInFluid(this.level().getFluidState(this.blockPosition())))
        {
            this.travelInFluid(input);
        }
        else
        if (IsFallFlying())
        {
            TravelFallFlying(input);
        }
        else
        {*/

        TravelInAir(input);

        //}
    }

    private void TravelInAir(Double3 input)
    {
        float friction = GetBlockFriction() * 0.91f;

        Double3 movement = HandleRelativeFrictionAndCalculateMovement(input, friction);

        movement.y -= GetGravity();

        deltaMovement = new(movement.x * friction, movement.y * 0.98f, movement.z * friction);
    }
    private Double3 HandleRelativeFrictionAndCalculateMovement(Double3 input, float friction)
    {
        MoveRelative(GetFrictionInfluencedSpeed(friction), input);

        Move(deltaMovement);
        Double3 movement = deltaMovement;

        /*
        if ((horizontalCollision || jumping) && (OnClimbable()))
        {
            movement = new(movement.x, 0.2, movement.z);
        }
        */

        return movement;
    }
    #endregion

    #region Private Methods

    #region Get
    private float GetBlockFriction()
    {
        // 0.6 Default
        // 0.8 Slime
        // 0.98 Ice
        // 1 Airborne
        return onGround ? 0.6f : 1f;
    }
    private float GetFrictionInfluencedSpeed(float blockFriction)
    {
        return onGround ? speed * (0.21600002F / (blockFriction * blockFriction * blockFriction)) : GetFlyingSpeed();
    }
    public virtual float GetFlyingSpeed()
    {
        return 0.02f;
    }
    #endregion

    #endregion
}