#version 450 core
layout(location = 0) out vec4 f_color;

in vec4 v_VertexColor;

void main()
{
    f_color = v_VertexColor; // Triangle Color Sample
    //f_color = vec4(fract(v_TexCoord), 0, 1); // UV Debug
    //f_color = texture(mainTexture, v_TexCoord) * vec4(v_VertexColor, 1); // Combination
}