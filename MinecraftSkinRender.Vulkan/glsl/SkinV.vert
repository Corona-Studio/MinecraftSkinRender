#version 450

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec2 a_texCoord;
layout(location = 2) in vec3 a_normal;

layout(set = 0, binding = 0) uniform UniformBufferObject {
    mat4 model;
    mat4 projection;
    mat4 view;
    mat4 self;
};

layout(location = 0) out vec3 normalIn;
layout(location = 1) out vec2 texIn;
layout(location = 2) out vec3 fragPosIn;

void main()
{
    texIn = a_texCoord;

    // 计算变换矩阵
    mat4 temp = view * model * self;

    fragPosIn = vec3(temp * vec4(a_position, 1.0));
    normalIn = mat3(transpose(inverse(temp))) * a_normal;

    gl_Position = projection * temp * vec4(a_position, 1.0);
}