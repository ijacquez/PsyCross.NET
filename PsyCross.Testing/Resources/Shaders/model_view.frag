#version 330

uniform sampler2D texture0;

uniform int is24BitDepth;

in vec2 texcoord;

out vec4 out_color;

vec4 rebuildColor(vec4 color)
{
    float r = floor((color.r * 255.0) / 8.0) / 31.0;
    float g = floor((color.g * 255.0) / 8.0) / 31.0;
    float b = floor((color.b * 255.0) / 8.0) / 31.0;

    return vec4(r, g, b, 1.0);
}

void main()
{
    if (is24BitDepth == 1) {
        out_color = texture(texture0, texcoord, 0);
    } else {
        out_color = rebuildColor(texture(texture0, texcoord, 0));
    }
}
