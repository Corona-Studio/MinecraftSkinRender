#version 450

layout(location = 0) in vec3 a_position;
layout(location = 1) in vec3 a_normal;
layout(location = 2) in vec2 a_texCoord;

layout(binding = 0) uniform UniformBufferObject {
    mat4 model;
    mat4 proj;
    mat4 view;
    mat4 self;
    vec3 light;
};

layout(location = 0) out vec3 normalIn;
layout(location = 1) out vec2 texIn;
layout(location = 2) out vec3 fragPosIn;
layout(location = 3) out vec3 lightColor;

void main()
{
    texIn = a_texCoord;

    // 计算变换矩阵
    mat4 temp = view * model * self;

    fragPosIn = vec3(temp * vec4(a_position, 1.0));
    normalIn = mat3(transpose(inverse(temp))) * a_normal;

    lightColor = light;
    gl_Position = proj * temp * vec4(a_position, 1.0);
}