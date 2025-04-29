using Silk.NET.Vulkan;

namespace MinecraftSkinRender.Vulkan.KHR;

public interface IVulkanApi
{
    public IReadOnlyList<string> GetRequiredExtensions();
    public SurfaceKHR CreateSurface(Instance instance);
}
