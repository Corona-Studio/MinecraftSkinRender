using Silk.NET.Vulkan;

namespace MinecraftSkinRender.Vulkan.KHR;

internal struct SwapChainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats;
    public PresentModeKHR[] PresentModes;
}