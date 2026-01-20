namespace Minecraft.Random;

/// <summary>
/// I have no clue why I went through all the trouble to implement this :sob:
/// </summary>
public abstract class RandomSource
{
    //public static RandomSource Create() => return Create(RandomSupport.generateUniqueSeed());

    public static RandomSource Create(long v) => new LegacyRandomSource(v);

    //public static RandomSource CreateNewThreadLocalInstance() => new SingleThreadedRandomSource(ThreadLocalRandom.current().nextLong());

    //public abstract RandomSource Fork();
    //public abstract PositionalRandomFactory ForkPositional();

    public abstract void SetSeed(long bounds);

    public abstract int NextInt();

    public abstract int NextInt(int v);

    public virtual int NextIntBetweenInclusive(int min, int max)
    {
        return NextInt(max - min + 1) + min;
    }

    public abstract long NextLong();

    public abstract bool NextBoolean();

    public abstract float NextFloat();

    public abstract double NextDouble();

    public abstract double NextGaussian();

    public virtual double Triangle(double min, double max)
    {
        return min + max * (NextDouble() - NextDouble());
    }

    public virtual float Triangle(float min, float max)
    {
        return min + max * (NextFloat() - NextFloat());
    }

    public virtual void ConsumeCount(int amount)
    {
        for (int i = 0; i < amount; i++) NextInt();
    }

    public virtual int NextInt(int origin, int max)
    {
        if (origin >= max)
        {
            throw new ArgumentException("bound - origin is non positive");
        }
        else
        {
            return origin + NextInt(max - origin);
        }
    }
}
