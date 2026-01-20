#version 450 core
layout(location = 0) out vec4 f_color;

in vec3 v_Normal;
in vec3 v_TexCoord;
in vec4 v_Color;

uniform sampler2DArray mainTexture;

void main()
{
    vec4 color = texture(mainTexture, v_TexCoord) * v_Color; // Texture Sample

    // Alpha Discard
    if (color.w < 0.1)
    {
        discard;
    }

    f_color = color; 

    //f_color = vec4(v_VertexColor, 1); // Triangle Color Sample
    //f_color = vec4(fract(v_TexCoord), 0, 1); // UV Debug
    //f_color = texture(mainTexture, v_TexCoord) * vec4(v_VertexColor, 1); // Combination
}