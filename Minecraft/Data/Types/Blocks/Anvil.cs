using BoboEngine.Utils;

namespace Minecraft.Blocks;

public class Anvil : ShapedBlock
{
    public Anvil() : base() { }
    public Anvil(string id, string name, DynamicData data) : base(id, name, data) { }
    public Anvil(string id, string name, string dataDir) : base(id, name, dataDir) { }
}
