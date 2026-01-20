#version 450 core
layout(location = 0) out vec4 f_color;

in vec3 v_Normal;
in vec2 v_TexCoord;
in float v_Phase;

uniform sampler2DArray mainTexture;

void main()
{
    f_color = texture(mainTexture, vec3(v_TexCoord, v_Phase));
}