#version 450

layout(binding = 1) uniform sampler2D texSampler;
layout(binding = 0) uniform UniformBufferObject {
    uniform vec3 lightColor;
};

layout(location = 0) in vec3 normalIn;
layout(location = 1) in vec2 texIn;
layout(location = 2) in vec3 fragPosIn;

layout(location = 0) out vec4 FragColor;

void main()
{
    float ambientStrength = 0.1f;
    vec3 ambient = ambientStrength * lightColor;

    vec3 norm = normalize(normalIn);
    vec3 lightDir = normalize(-fragPosIn);
    float diff = max(dot(norm, lightDir), 0.0f);
    vec3 diffuse = diff * lightColor;

    vec3 result = (ambient + diffuse);

    FragColor = texture(texSampler, texIn) * vec4(result, 1.0f);
}