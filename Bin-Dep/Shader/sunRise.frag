#version 450 core
layout(location = 0) out vec4 f_color;

in vec4 modulator;
in vec4 v_Color;

void main()
{
    if (v_Color.a == 0.0)
    {
        discard;
    }

    f_color = v_Color * modulator;
}