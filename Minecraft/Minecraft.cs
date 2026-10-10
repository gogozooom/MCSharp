using BoboEngine;
using BoboEngine.Utils;
using ConsoleCommand;
using Minecraft.Entites;
using Minecraft.UI;
using Minecraft.World;

namespace Minecraft;

public static class Minecraft
{
    public static bool frozen = false;
    public static int tick { get; private set; }
    public static int tickRate = 20;
    public const int ticksPerSecond = 20;
    public static float partialTickTime { get; private set; }

    public static Action onTick;
    private static Task ticker;

    [OnEngineInitialize]
    public static void Initialize()
    {
        if (!VerifyMCDataVersion()) return;

        TextureManager.GenerateAtlas();
        BlockTypeManager.GenerateBlockData();

        Engine.Log("Minecraft Initialized!");

        SceneManager.sceneLoaded += OnSceneLoad;
    }

    const string WorkingVersion = "1.21.10";
    static bool VerifyMCDataVersion()
    {
        if (!Directory.Exists("Data"))
        {
            Engine.LogError($"Could not find Minecraft Data! Please extract version {WorkingVersion} data in folder named \"Data\" next to the executable!");
            return false;
        }

        if (!File.Exists("Data\\version.json"))
        {
            Engine.LogError("Could not find version.json in Data folder!");
            return false;
        }
        
        var json = FileParser.ParseJson("Version", File.ReadAllText("Data\\version.json"));
        var id = json.GetItem("id");

        if (id == null)
        {
            Engine.LogError("Could not id property in version.json!");
            return false;
        }

        var idS = id.GetValue<String>();

        Engine.Log($"Minecraft Version: {idS}");

        if (idS != WorkingVersion)
        {
            Engine.LogWarning($"Only Minecraft version {WorkingVersion} is tested to work!");
        }

        return true;
    }

    public static void OnSceneLoad()
    {
        WorldChunkManager.GenerateTestChunks();
        SceneManager.currentScene.skybox.gameObject.AddComponent<Sky>();
        new GameObject("UIManager").AddComponent<UIManager>();
        new GameObject("Player").AddComponent<Player>();

        if (ticker != null)
        {
            Engine.LogError("Cannot start another tick timer!");
            return;
        }
        ticker = Task.Run(Ticker);
    }

    public static void Ticker()
    {
        var lastTicked = Time.time;

        while (true)
        {
            partialTickTime = (Time.time - lastTicked) / (1f / tickRate);

            if (partialTickTime >= 1)
            {
                lastTicked = Time.time;
                tick++;
                try
                {
                    onTick?.Invoke();
                }
                catch (Exception ex)
                {
                    Engine.LogError($"{ex.Message}");
                }
            }

            while (frozen);
        }
    }

    [Command("tick")]
    public static void TickCommand(string type, int value)
    {
        switch (type)
        {
            case null:
            case "":
                Engine.Log($"Current tick rate is '{tickRate}'");
                break;
            case "rate":
                tickRate = value;
                Engine.Log($"Tick rate set to '{tickRate}'");
                break;
            case "freeze":
                frozen = true;
                Engine.Log($"Tick rate froze set to '{frozen}'");
                break;
            case "unfreeze":
                frozen = false;
                Engine.Log($"Tick rate froze set to '{frozen}'");
                break;
            default:
                Engine.LogWarning($"Command type: '{type}' not recogized!");
                break;
        }
    }
}