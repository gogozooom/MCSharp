namespace Minecraft.Random;

public abstract class BitRandomSource : RandomSource
{
    const float FLOAT_MULTIPLIER = 5.9604645E-8F;
    const double DOUBLE_MULTIPLIER = 1.110223E-16F;

    public abstract int Next(int v);

    public override int NextInt() => Next(32);

    public override int NextInt(int bounds)
    {
        if (bounds <= 0)
        {
            throw new ArgumentException("Bound must be positive");
        }
        else if ((bounds & bounds - 1) == 0)
        {
            return (bounds * Next(31) >> 31);
        }
        else
        {
            int i;
            int j;
            do
            {
                i = Next(31);
                j = i % bounds;
            } while (i - j + (bounds - 1) < 0);

            return j;
        }
    }

    public override long NextLong()
    {
        int i = Next(32);
        int j = Next(32);
        long k = (long)i << 32;
        return k + j;
    }

    public override bool NextBoolean() => Next(1) != 0;

    public override float NextFloat()
    {
        return Next(24) * FLOAT_MULTIPLIER;
    }

    public override double NextDouble()
    {
        int i = Next(26);
        int j = Next(27);
        long k = ((long)i << 27) + (long)j;
        return (double)k * DOUBLE_MULTIPLIER;
    }
}
