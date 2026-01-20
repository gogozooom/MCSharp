#version 450 core
layout (location = 0) in vec3 a_Position;

out float sphericalVertexDistance;
out float cylindricalVertexDistance;
out vec3 v_skyColor;
out vec3 v_fogColor;

uniform mat4 projection;
uniform mat4 model;
uniform vec3 skyColor;
uniform vec3 fogColor;

float fog_spherical_distance(vec3 pos) {
    return length(pos);
}

float fog_cylindrical_distance(vec3 pos) {
    float distXZ = length(pos.xz);
    float distY = abs(pos.y);
    return max(distXZ, distY);
}

void main()
{
    gl_Position = projection * model * vec4(a_Position, 1.0); // position x, y, z, 1

    sphericalVertexDistance = fog_spherical_distance(a_Position);
    cylindricalVertexDistance = fog_cylindrical_distance(a_Position);
    v_skyColor = skyColor;
    v_fogColor = fogColor;
}