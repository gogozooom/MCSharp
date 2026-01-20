namespace Minecraft.Random;

public class MarsagliaPolarGaussian
{
    public readonly RandomSource randomSource;
    private double nextNextGaussian;
    private bool haveNextNextGaussian;

    public MarsagliaPolarGaussian(RandomSource source)
    {
        randomSource = source;
    }

    public void Reset()
    {
        haveNextNextGaussian = false;
    }

    public double NextGaussian()
    {
        if (haveNextNextGaussian)
        {
            haveNextNextGaussian = false;
            return nextNextGaussian;
        }
        else
        {
            double v1;
            double v2;
            double v3;
            do
            {
                v1 = 2.0 * randomSource.NextDouble() - 1.0;
                v2 = 2.0 * randomSource.NextDouble() - 1.0;
                v3 = v1 * v1 + v2 * v2;
            } while (v3 >= 1.0 || v3 == 0.0);

            double v4 = Math.Sqrt(-2.0 * Math.Log(v3) / v3);
            nextNextGaussian = v2 * v4;
            haveNextNextGaussian = true;
            return v1 * v4;
        }
    }
}
