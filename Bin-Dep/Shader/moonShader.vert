#version 450 core
layout (location = 0) in vec3 a_Position;
layout (location = 1) in vec3 a_Normal;
layout (location = 2) in vec2 a_TexCoord;

out vec3 v_Normal;
out vec2 v_TexCoord;
out float v_Phase;

uniform mat4 projection;
uniform mat4 model;
uniform float phase;

void main()
{
    gl_Position = projection * model * vec4(a_Position, 1.0); // position x, y, z, 1
    v_Normal = a_Normal;
    v_TexCoord = a_TexCoord;
    v_Phase = phase;
}