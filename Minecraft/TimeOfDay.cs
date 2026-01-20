using BoboEngine;
using ConsoleCommand;

namespace Minecraft;

public static class TimeOfDay
{
    public static int time;

    [OnEngineInitialize]
    public static void Initialize()
    {
        Minecraft.onTick += OnTick;
    }

    public static void OnTick()
    {
        time += 1;
    }

    /// <summary>
    /// A value from 0 - 1 starting when the sun is at the top of the sky
    /// </summary>
    public static float GetTimeOfDay()
    {
        float mod = Maths.Mod(time / 24000f - 0.25f, 1);

        float sinOffset = 0.5f - Maths.Cos(mod * 180) / 2;

        return (mod * 2 + sinOffset) / 3;
    }

    [Command("time", "['type', #value] Settings releated to the time")]
    public static void OnCommand(string type, int value)
    {
        switch (type)
        {
            case null: case "":
                Engine.Log($"Current time is '{time}'");
                break;
            case "set":
                time = value;
                Engine.Log($"Time set to '{time}'");
                break;
            case "add":
                time += value;
                Engine.Log($"Time set to '{time}'");
                break;
            default:
                Engine.LogWarning($"Command type: '{type}' not recogized!");
                break;
        }
    }
}