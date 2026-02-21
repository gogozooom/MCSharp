using BoboEngine;
using static OpenGL.GL;

namespace Minecraft.World;

public class ChunkMesh : Mesh
{
    public new ChunkFaceInfo[] faces;

    public void LoadRawData(Float3[] vertices, ChunkFaceInfo[] faces = null, Float3[] normals = null, Float2[] textureCoords = null)
    {
        if (!IsEmpty())
        {
            Engine.LogWarning("Cannot load data! Data is already loaded!");
            return;
        }

        this.vertices = vertices;
        this.faces = faces ?? ([]);
        this.normals = normals ?? ([]);
        this.textureCoords = textureCoords ?? ([]);
    }

    protected override (float[] data, uint vertexBufferSize) glConvertToData()
    {
        var vertexData = new float[faces.Length * 39];

        for (int i = 0; i < faces.Length; i++)
        {
            var face = faces[i];

            var faceIndex = i * 39; // 13 * 3

            for (int vertexI = 0; vertexI < 3; vertexI++)
            {
                float lightLevel = face.occlusionLevel[vertexI];
                Float3 position = vertices[face.vertex_indexs[vertexI]];
                Float3 normal = normals[face.normal_indexs[vertexI]];
                Float2 uv = new(0, 0);

                if (textureCoords.Length > 0)
                    uv = textureCoords[face.texture_indexs[vertexI]];

                vertexData[faceIndex + 0] = position.x;
                vertexData[faceIndex + 1] = position.y;
                vertexData[faceIndex + 2] = position.z;

                vertexData[faceIndex + 3] = normal.x;
                vertexData[faceIndex + 4] = normal.y;
                vertexData[faceIndex + 5] = normal.z;

                vertexData[faceIndex + 6] = uv.x;
                vertexData[faceIndex + 7] = uv.y;
                vertexData[faceIndex + 8] = face.texture_id;

                vertexData[faceIndex + 9] = lightLevel;
                vertexData[faceIndex + 10] = lightLevel;
                vertexData[faceIndex + 11] = lightLevel;
                vertexData[faceIndex + 12] = 1;

                faceIndex += 13;
            }
        }

        return (vertexData, (uint)faces.Length * 3);
    }
    protected override unsafe void glBindPointers()
    {
        // Position (x,y,z)
        glVertexAttribPointer(0, 3, GL_FLOAT, false, 13 * sizeof(float), (void*)0);
        glEnableVertexAttribArray(0);

        // Normals (x,y,z)
        glVertexAttribPointer(1, 3, GL_FLOAT, false, 13 * sizeof(float), (void*)(3 * sizeof(float)));
        glEnableVertexAttribArray(1);

        // Vertex Texture Coords (u,v,i)
        glVertexAttribPointer(2, 3, GL_FLOAT, false, 13 * sizeof(float), (void*)(6 * sizeof(float)));
        glEnableVertexAttribArray(2);

        // Vertex Color (r,g,b,a)
        glVertexAttribPointer(3, 4, GL_FLOAT, false, 13 * sizeof(float), (void*)(9 * sizeof(float)));
        glEnableVertexAttribArray(3);
    }

    public override bool IsEmpty()
    {
        if (vertices == null) return true;
        else if (vertices.Length == 0) return true;

        if (faces == null) return true;
        else if (faces.Length == 0) return true;

        return false;
    }
}
public class ChunkFaceInfo : FaceInfo
{
    public int texture_id;
    public List<float> occlusionLevel = new();

    public ChunkFaceInfo(string objFaceElements) : base(objFaceElements) {}

    // This is kinda stupid
    public new static ChunkFaceInfo[] GetTriangulatedFaces(string objFaceElements)
    {
        string[] elements = objFaceElements.Split(' ')[1..];

        List<ChunkFaceInfo> faces = new();

        if (elements.Length <= 3)
        {
            faces = [new(objFaceElements)];
        }
        else
        {
            for (int i = 2; i < elements.Length; i++)
            {
                faces.Add(new($"f {elements[0]} {elements[i - 1]} {elements[i]}"));
            }
        }

        return faces.ToArray();
    }
}