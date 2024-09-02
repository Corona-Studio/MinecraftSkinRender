using System.Numerics;

namespace MinecraftSkinRender.Vulkan;

public struct UniformBufferObject
{
    public Matrix4x4 model;
    public Matrix4x4 proj;
    public Matrix4x4 view;
    public Matrix4x4 self;
    public Vector3 lightColor;
}