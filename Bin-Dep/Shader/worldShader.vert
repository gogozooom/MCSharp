#version 450 core
layout (location = 0) in vec3 a_Position;
layout (location = 1) in vec3 a_Normal;
layout (location = 2) in vec3 a_TexCoord;
layout (location = 3) in vec4 a_Color;

out vec3 v_Normal;
out vec3 v_TexCoord;
out vec4 v_Color;

uniform mat4 projection;
uniform mat4 model;

void main()
{
    gl_Position = projection * model * vec4(a_Position, 1.0); // position x, y, z, 1
    v_Normal = a_Normal;
    v_TexCoord = a_TexCoord;
    v_Color = a_Color;
}