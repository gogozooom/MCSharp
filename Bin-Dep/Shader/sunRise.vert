#version 450 core
layout (location = 0) in vec3 a_Position;
layout (location = 1) in vec4 a_Color;

out vec4 v_Color;
out vec4 modulator;

uniform mat4 projection;
uniform mat4 model;
uniform vec4 color;

void main()
{
    gl_Position = projection * model * vec4(a_Position, 1.0); // position x, y, z, 1
    v_Color = a_Color;
    modulator = color;
}