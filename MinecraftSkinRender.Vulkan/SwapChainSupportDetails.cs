using Silk.NET.Vulkan;

namespace MinecraftSkinRender.Vulkan;

internal struct SwapChainSupportDetails
{
    public SurfaceCapabilitiesKHR Capabilities;
    public SurfaceFormatKHR[] Formats;
    public PresentModeKHR[] PresentModes;
}