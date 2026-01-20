using BoboEngine.Utils;
using Minecraft.Blocks;

namespace Minecraft;
public static class BlockTypeManager
{
    public static Dictionary<string, BaseBlock> blockData { get; private set; } = new();

    public static void GenerateBlockData()
    {
        blockData.Clear();

        BaseBlock[] blocks = [ // TODO: Move to json files
            new Block("minecraft:air", "Air", MinecraftJsonManager.GetData("models/block/air")),
            new FullBlock(), // Null
            new FullBlock("minecraft:bedrock", "Bedrock", "models/block/bedrock"),
            new FullBlock("minecraft:dirt", "Dirt", "models/block/dirt"),
            new FullBlock("minecraft:grass_block", "Grass Block", "models/block/grass_block"),
            new FullBlock("minecraft:cobblestone", "Cobblestone", "models/block/cobblestone"),
            new FullBlock("minecraft:stone", "Stone", "models/block/stone"),
            new FullBlock("minecraft:oak_log", "Oak Log", "models/block/oak_log"),
            new LeafBlock("minecraft:oak_leaves", "Oak Leaves", "models/block/oak_leaves"),
            new Slab("minecraft:oak_slab", "Oak Slab", "models/block/oak_slab"),
            new ShapedBlock("minecraft:oak_stairs", "Oak Stairs", "models/block/oak_stairs"),
            new FullBlock("minecraft:oak_planks", "Oak Planks", "models/block/oak_planks"),
            new FullBlock("minecraft:crafting_table", "Crafting Table", "models/block/crafting_table"),
            new FullBlock("minecraft:orange_wool", "Orange Wool", "models/block/orange_wool"),
            new FullBlock("minecraft:blue_wool", "Blue Wool", "models/block/blue_wool"),
            new FullBlock("minecraft:black_wool", "Black Wool", "models/block/black_wool"),
            new FullBlock("minecraft:white_wool", "White Wool", "models/block/white_wool"),
            new Anvil("minecraft:anvil", "Anvil", "models/block/anvil"),
            new Block("minecraft:azalea", "Azalea", "models/block/azalea"),
            new Block("minecraft:beacon", "Beacon", "models/block/beacon"),
            new Torch("minecraft:torch", "Torch", "models/block/torch"),
            new TransparentBlock("minecraft:glass", "Glass", "models/block/glass"),


            /*
            new TransparentBlock("minecraft:orange_stained_glass", "Orange Stained Glass", MinecraftJsonManager.GetData("models/block/orange_stained_glass")),
            new TransparentBlock("minecraft:pink_stained_glass", "Pink Stained Glass", MinecraftJsonManager.GetData("models/block/pink_stained_glass")),
            new TransparentBlock("minecraft:green_stained_glass", "Green Stained Glass", MinecraftJsonManager.GetData("models/block/green_stained_glass")),
            new TransparentBlock("minecraft:yellow_stained_glass", "Yellow Stained Glass", MinecraftJsonManager.GetData("models/block/yellow_stained_glass"))
            */
            ];

        foreach (var block in blocks)
        {
            blockData.Add(block.id, block);
        }

        /* Old Method

        blockData.Add("minecraft:null", new CubeBlock());

        string[] watchList = [
            // Normal Blocks
            "models/block/air",
            "models/block/dirt",
            "models/block/grass_block",
            "models/block/rose_bush_bottom",
            "models/block/rose_bush_top",
            "models/block/anvil",
            "models/block/crafting_table",

            "models/block/oak_button",
            "models/block/oak_button_inventory",
            
            // Broken Blocks
            "models/block/oak_log_horizontal", // Not horizontal?
            "models/block/lantern", // No Textures?

            // Fix animated textures
            "models/block/magma_block",
            "models/block/command_block",

            // Fix textures bigger than 16x16
            "models/block/cherry_shelf_inventory",

            // Better Transparency Support
            "models/block/orange_stained_glass",
            "models/block/pink_stained_glass",
            "models/block/green_stained_glass",
            "models/block/yellow_stained_glass",
            
            ];

        foreach (var blockV in watchList)
        {
            var data = MinecraftJsonManager.GetData(blockV);

            if (!data)
            {
                Program.LogWarning($"Could not find: '{blockV}'!");
                continue;
            }

            AddBlockData(data);
        }
        */
    }

    private static void AddBlockData(DynamicData data)
    {
        MinecraftJsonManager.FullyPopulateData(ref data, "models/");

        var name = data.name.Split('/')[^1];

        var id = "minecraft:" + name;

        blockData.Add(id, new Block(id, name, data));
    }
    public static BaseBlock GetBlockType(string id)
    {
        if (id != null && blockData.ContainsKey(id))
        {
            return blockData[id];
        }

        return blockData["minecraft:null"];
    }
}