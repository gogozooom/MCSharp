using BoboEngine;
using static OpenGL.GL;

namespace Minecraft.World;

public class ChunkMesh : Mesh
{
    public int[] textureIdIndices; // The texture id to put on a face
    public Float4[] occlusionLevels;

    public void LoadRawData(Float3[] vertices, MeshFace[] faces = null, Float3[] normals = null, Float2[] textureCoords = null, int[] textureIdIndices = null, Float4[] occlusionLevels = null)
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
        this.textureIdIndices = textureIdIndices ?? ([]);
        this.occlusionLevels = occlusionLevels ?? ([]);
    }

    protected override (float[] data, uint vertexBufferSize) glConvertToData()
    {
        var triangulatedFaces = GetTriangulatedFaces();

        var vertexData = new float[triangulatedFaces.Length * 39];

        int fI = 0;
        for (int i = 0; i < faces.Length; i++)
        {
            var lightLevels = occlusionLevels[i];

            foreach (var face in faces[i].GetFaceTriangulated())
            {
                var faceIndex = fI * 39; // 13 * 3

                for (int vertexI = 0; vertexI < 3; vertexI++)
                {
                    var vertex = face.vertices[vertexI];

                    float lightLevel;

                    // What on earth is going on here?
                    // Idk, blame it on structs not being extendable...
                    // To fix this I'd have to package occlusionLevels with each face or smth
                    // TODO: either figure out a more elegant way to do this, or just replace MeshFace and MeshVertex with custom ones...
                    if (Maths.Mod(fI, 2) == 0)
                    {
                        lightLevel = vertexI switch
                        {
                            0 => lightLevels.x, // Bottom Left
                            1 => lightLevels.y, // Bottom Right
                            _ => lightLevels.z, // Top Right
                        };
                    }
                    else
                    {
                        lightLevel = vertexI switch
                        {
                            0 => lightLevels.x, // Bottom Left
                            1 => lightLevels.z, // Top Right
                            _ => lightLevels.w, // Top Left
                        };
                    }

                    Float3 position = vertices[vertex.vertex_coord_index];
                    Float3 normal = vertex.normal_coord_index >= 0 ? normals[vertex.normal_coord_index] : new();
                    Float2 uv = vertex.texture_coord_index >= 0 ? textureCoords[vertex.texture_coord_index] : new(0, 0);

                    vertexData[faceIndex + 0] = position.x;
                    vertexData[faceIndex + 1] = position.y;
                    vertexData[faceIndex + 2] = position.z;

                    vertexData[faceIndex + 3] = normal.x;
                    vertexData[faceIndex + 4] = normal.y;
                    vertexData[faceIndex + 5] = normal.z;

                    vertexData[faceIndex + 6] = uv.x;
                    vertexData[faceIndex + 7] = uv.y;
                    vertexData[faceIndex + 8] = textureIdIndices[i];

                    vertexData[faceIndex + 9] = lightLevel;
                    vertexData[faceIndex + 10] = lightLevel;
                    vertexData[faceIndex + 11] = lightLevel;
                    vertexData[faceIndex + 12] = 1;

                    faceIndex += 13;
                }

                fI++;
            }
        }

        return (vertexData, (uint)triangulatedFaces.Length * 3);
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