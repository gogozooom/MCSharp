#version 450 core
layout (location = 0) in vec3 a_Position;

out vec4 color;

uniform mat4 projection;
uniform mat4 model;
uniform float brightness;

void main()
{
    gl_Position = projection * model * vec4(a_Position, 1.0); // position x, y, z, 1
   
    color = vec4(brightness);
}