using BoboEngine;
using BoboEngine.Utils;
using Minecraft.Blocks;

namespace Minecraft;

public static class MinecraftJsonManager
{
    public static Dictionary<string, DynamicData> allData = new();

    public static DynamicData AddFile(string id)
    {
        var file = Path.Combine(Engine.ProgramDirectory, "Data\\assets\\minecraft", id + ".json").Replace('/','\\');

        if (!File.Exists(file))
        {
            Engine.LogError($"Could not find raw block model data for id '{id}'! Make sure minecraft data named as 'Data' is in the executable directory!");
            return BaseBlock.nullData;
        }

        var data = File.ReadAllText(file);

        var results = FileParser.ParseJson(data, id);

        allData.Add(id, results);

        return results;
    }
    public static DynamicData GetData(string id)
    {
        if (!allData.TryGetValue(id, out var result))
        {
            return AddFile(id);
        }

        return result;
    }

    public static DynamicData FullyPopulateData(ref DynamicData data, string context = "")
    {
        while (data.HasItem("parent"))
        {
            var parentV = context + data.GetItem("parent").GetValue<string>().Split(':')[^1];

            var parentD = GetData(parentV);

            if (!parentD)
            {
                Engine.LogWarning($"Parent '{parentV}' does not exist!");
                return data;
            }

            data.Merge(parentD);

            if (!parentD.HasItem("parent")) break;
        }

        data.RemoveItem("parent");

        PopulateReferences(ref data);

        return data;
    }

    public static void PopulateReferences(ref DynamicData data)
    {
        var texturesD = data.GetItem("textures");

        if (!texturesD) return;

        var elementsD = data.GetItem("elements");

        if (!elementsD) return;

        Dictionary<string, DynamicData> independentValues = new();
        List<DynamicData> dependentValues = new();

        foreach (var value in texturesD.GetAllValues(true))
        {
            if (value.GetDataType() == DataType.Value && value.GetValue<string>().Contains('#'))
            {
                dependentValues.Add(value);
            }
            else
            {
                if (independentValues.ContainsKey(value.name))
                {
                    //Program.LogWarning($"Duplicate values '{value.name}'");
                    continue;
                }

                independentValues.Add(value.name, value);
            }
        }

        foreach (var value in elementsD.GetAllValues())
        {
            if (value.GetDataType() == DataType.Value && value.GetValue<string>().Contains('#'))
            {
                dependentValues.Add(value);
            }
        }

        bool changeMade = false;

        foreach (var value in dependentValues.ToArray())
        {
            var indepS = value.GetValue<string>().Remove(0, 1); // Remove #

            if(!independentValues.TryGetValue(indepS, out var indepD)) continue;

            changeMade = true;
            if(indepD.GetDataType() == DataType.Array)
            {
                value.SetValue(indepD);
            }
            else
            {
                value.SetValue(indepD.GetValue<string>());
            }
            dependentValues.Remove(value);
        }

        if (!changeMade)
        {
            if(dependentValues.Count > 0)
            {
                Engine.LogWarning($"Some properties in '{data}' where not populated!");
            }
            return;
        }

        // Repeat
        PopulateReferences(ref data);
    }
}