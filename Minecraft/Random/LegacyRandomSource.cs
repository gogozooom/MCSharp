namespace Minecraft.Random;

public class LegacyRandomSource : BitRandomSource
{
    //private const int MODULUS_BITS = 48;
    private const long MODULUS_MASK = 281474976710655L;
    private const long MULTIPLIER = 25214903917L;
    private const long INCREMENT = 11L;

    private readonly AtomicLong seed = new AtomicLong();
    private readonly MarsagliaPolarGaussian gaussianSource;

    public LegacyRandomSource(long seed)
    {
        gaussianSource = new MarsagliaPolarGaussian(this);
        SetSeed(seed);
    }

    /*
    public override RandomSource Fork() => new LegacyRandomSource(NextLong());
    public override PositionalRandomFactory ForkPositional() => new LegacyRandomSource.LegacyPositionalRandomFactory(NextLong());
    */

    public override void SetSeed(long seed)
    {
        this.seed.Set((seed ^ MULTIPLIER) & MODULUS_MASK);
        this.gaussianSource.Reset();
    }

    public override int Next(int v)
    {
        var value = seed.Get() * MULTIPLIER + INCREMENT & MODULUS_MASK;

        seed.Set(value);

        return (int)(value >> 48 - v);
    }

    public override double NextGaussian()
    {
        return gaussianSource.NextGaussian();
    }
}