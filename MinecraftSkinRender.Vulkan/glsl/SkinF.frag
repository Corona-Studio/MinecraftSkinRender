#version 450

layout(binding = 1) uniform sampler2D texSampler;

layout(location = 0) in vec3 normalIn;
layout(location = 1) in vec2 texIn;
layout(location = 2) in vec3 fragPosIn;
layout(location = 3) in vec3 lightColor;

layout(location = 0) out vec4 outColor;

void main()
{
    float ambientStrength = 0.1f;
    vec3 ambient = ambientStrength * lightColor;

    vec3 norm = normalize(normalIn);
    vec3 lightDir = normalize(-fragPosIn);
    float diff = max(dot(norm, lightDir), 0.0f);
    vec3 diffuse = diff * lightColor;

    vec3 result = (ambient + diffuse);

    outColor = texture(texSampler, texIn) * vec4(result, 1.0f);
    
    //outColor = texture(texSampler, texIn);
}