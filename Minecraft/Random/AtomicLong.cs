namespace Minecraft.Random;

public class AtomicLong
{
    private long value;
    public long Get() => value;
    public void Set(long newValue) => value = newValue;

    public AtomicLong() { }
    public AtomicLong(long initialValue)
    {
        value = initialValue;
    }

}
