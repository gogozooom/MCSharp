using BoboEngine;
using BoboEngine.Utils;

namespace Minecraft;
public class BlockModel
{
    public BlockModelElement[] elements;

    public BlockModel(BlockModelElement[] elements)
    {
        this.elements = elements;
    }

    public static BlockModel LoadFromJson(DynamicData data)
    {
        List<BlockModelElement> elements = new();

        var elementsD = data.GetItem("elements");

        if (elementsD)
        {
            foreach (var element in elementsD.GetImmediateChildren())
            {
                elements.Add(BlockModelElement.LoadFromJson(element));
            }
        }

        return new(elements.ToArray());
    }

    public static implicit operator bool(BlockModel v) => v != null;
}

public class BlockModelElement
{
    public readonly Float3 from;
    public readonly Float3 to;

    public readonly BlockModelRotation rotation;

    public readonly bool shade;

    public readonly BlockModelFace down;
    public readonly BlockModelFace up;
    public readonly BlockModelFace north;
    public readonly BlockModelFace south;
    public readonly BlockModelFace west;
    public readonly BlockModelFace east;

    public readonly bool HasFaceData;

    public BlockModelElement(Float3 from, Float3 to, BlockModelRotation rotation, bool shade, BlockModelFace down, BlockModelFace up, BlockModelFace north, BlockModelFace south, BlockModelFace west, BlockModelFace east)
    {
        this.from = from;
        this.to = to;

        this.rotation = rotation;

        this.shade = shade;

        this.down = down;
        this.up = up;
        this.north = north;
        this.south = south;
        this.west = west;
        this.east = east;

        HasFaceData = down || up || north || south || west || east;
    }

    public static BlockModelElement LoadFromJson(DynamicData elementData)
    {
        Float3 from = Float3.zero;
        Float3 to = Float3.zero;

        BlockModelRotation rotation;

        bool shade = true;

        BlockModelFace down = null;
        BlockModelFace up = null;
        BlockModelFace north = null;
        BlockModelFace south = null;
        BlockModelFace west = null;
        BlockModelFace east = null;


        var fromD = elementData.GetItem("from");
        if (fromD) from = fromD.GetValue<Float3>() / 16;

        var toD = elementData.GetItem("to");
        if (toD) to = toD.GetValue<Float3>() / 16;

        rotation = BlockModelRotation.LoadFromJson(elementData.GetItem("rotation"));

        var shadeD = elementData.GetItem("shade");
        if (shadeD) shade = shadeD.GetValue<bool>();

        var facesD = elementData.GetItem("faces");

        if (facesD)
        {
            down = BlockModelFace.LoadFromJson(facesD.GetItem("down"), from, to);
            up = BlockModelFace.LoadFromJson(facesD.GetItem("up"), from, to);
            north = BlockModelFace.LoadFromJson(facesD.GetItem("north"), from, to);
            south = BlockModelFace.LoadFromJson(facesD.GetItem("south"), from, to);
            west = BlockModelFace.LoadFromJson(facesD.GetItem("west"), from, to);
            east = BlockModelFace.LoadFromJson(facesD.GetItem("east"), from, to);
        }

        return new(from, to, rotation, shade, down, up, north, south, west, east);
    }

    Float3[] _cachedVerts;
    public Float3[] GetCachedVerticies()
    {
        return _cachedVerts;
    }
    public void CacheVerticies(Float3[] v) => _cachedVerts = v;

    public static implicit operator bool(BlockModelElement v) => v != null;
}

public class BlockModelRotation
{
    public readonly Float3 origin;
    public readonly Axis axis;
    public readonly float angle;
    public readonly bool rescale;

    public BlockModelRotation(Float3 origin, Axis axis, float angle, bool rescale)
    {
        this.origin = origin;
        this.axis = axis;
        this.angle = angle;
        this.rescale = rescale;
    }

    public static BlockModelRotation LoadFromJson(DynamicData rotationData)
    {
        if (rotationData == null) return null;

        Float3 origin = new(0, 0, 0);
        Axis axis = Axis.Yaxis;
        float angle = 0;
        bool rescale = false;

        var angleD = rotationData.GetItem("angle");
        if (angleD) angle = angleD.GetValue<float>();

        // Avoid unnesesary calculations
        if (angle == 0) return null;

        var originD = rotationData.GetItem("origin");
        if (originD) origin = originD.GetValue<Float3>() / 16;

        var axisD = rotationData.GetItem("axis");
        if (axisD) axis = axisD.GetValue<Axis>();

        var rescaleD = rotationData.GetItem("rescale");
        if (rescaleD) rescale = rescaleD.GetValue<bool>();

        return new(origin, axis, angle, rescale);
    }


    public static implicit operator bool(BlockModelRotation v) => v != null;
}

public class BlockModelFace
{
    public readonly UVRect uv;
    public readonly string texture;
    public readonly float rotation;
    public readonly string cullface;

    public BlockModelFace(UVRect uv, string texture, float rotation, string cullface)
    {
        this.uv = uv;
        this.texture = texture;
        this.rotation = rotation;
        this.cullface = cullface;
    }

    public static BlockModelFace LoadFromJson(DynamicData faceData, Float3 from, Float3 to)
    {
        if (faceData == null) return null;

        UVRect uv;
        string texture = null;
        float rotation = 0;
        string cullface = null;

        var uvD = faceData.GetItem("uv");
        if (uvD)
            uv = uvD.GetValue<UVRect>() / 16;
        else
        {
            // Calculate UVS

            Float3 fromF = 1 - from;

            Float3 size = to - from;

            Float3 sizeF = 1 - size;

            switch (faceData.name)
            {
                case "east":

                    uv = new UVRect(sizeF.z - from.z, sizeF.y - from.y, fromF.z, fromF.y);

                    break;
                case "west":

                    uv = new UVRect(from.z, sizeF.y - from.y, from.z + size.z, fromF.y);

                    break;
                case "down":

                    uv = new UVRect(from.x, sizeF.z - from.z, from.x + size.x, size.z + from.z);

                    break;
                case "up":

                    uv = new UVRect(from.x, from.z, size.x + from.x, size.z + from.z);

                    break;
                case "north":

                    uv = new UVRect(sizeF.x - from.x, sizeF.y - from.y, fromF.x, fromF.y);

                    break;
                default: case "south":

                    uv = new UVRect(from.x, sizeF.y - from.y, from.x + size.x, fromF.y);

                    break;
            }
        }

        // Minecraft model UVs go:
        // from: 0, 0  (top left)
        // to:   16,16 (bottom right)

        // But 3D model UVs go:
        // from: 0, 0  (bottom left)
        // to:   16,16 (top right)

        uv = uv.FlipV();

        var textureD = faceData.GetItem("texture");
        if (textureD) texture = textureD.GetValue<string>();

        var rotateD = faceData.GetItem("rotation");
        if (rotateD)
        {
            var rotateV = rotateD.GetValue<int>();

            if (rotateV == 0 || rotateV == 90 || rotateV == 180 || rotateV == 270)
            {
                rotation = rotateV;
            }
            else
            {
                Engine.LogWarning($"Invalid texture rotation value '{rotateV}'");
            }
        }

        var cullfaceD = faceData.GetItem("cullface");
        if (cullfaceD) cullface = cullfaceD.GetValue<string>();

        return new(uv, texture, rotation, cullface);
    }

    public static implicit operator bool(BlockModelFace v) => v != null;
}