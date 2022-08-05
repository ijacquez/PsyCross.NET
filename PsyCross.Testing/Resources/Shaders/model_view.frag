#version 330

uniform sampler2D texture0;

in vec2 texcoord;

out vec4 out_color;

void main()
{
    out_color = texture(texture0, texcoord, 0);
}
