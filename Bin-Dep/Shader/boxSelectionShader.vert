#version 450 core
layout (location = 0) in vec3 a_Position;
layout (location = 1) in vec3 a_Normal;

out vec4 v_VertexColor;

uniform mat4 projection;
uniform mat4 model;

uniform vec2 ScreenSize;
uniform vec4 Color;
const float LineWidth = 4;

// Minecraft Block Outline Shader
void main() {
    vec4 linePosStart = projection * model * vec4(a_Position, 1.0);
    vec4 linePosEnd = projection * model * vec4(a_Position + a_Normal, 1.0);

    vec3 ndc1 = linePosStart.xyz / linePosStart.w;
    vec3 ndc2 = linePosEnd.xyz / linePosEnd.w;

    vec2 lineScreenDirection = normalize((ndc2.xy - ndc1.xy) * ScreenSize);
    vec2 lineOffset = vec2(-lineScreenDirection.y, lineScreenDirection.x) * LineWidth / ScreenSize;

    if (lineOffset.x < 0.0)
    {
        lineOffset *= -1.0;
    }

    if (gl_VertexID % 2 == 0)
    {
        gl_Position = vec4((ndc1 + vec3(lineOffset, 0.0)) * linePosStart.w, linePosStart.w);
    } else
    {
        gl_Position = vec4((ndc1 - vec3(lineOffset, 0.0)) * linePosStart.w, linePosStart.w);
    }

    v_VertexColor = Color;
    //v_VertexColor = vec4(a_Normal,1); Debug Normals
}