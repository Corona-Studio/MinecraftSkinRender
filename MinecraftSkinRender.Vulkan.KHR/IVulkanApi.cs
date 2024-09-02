using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;

namespace MinecraftSkinRender.Vulkan.KHR;

public interface IVulkanApi
{
    public IReadOnlyList<string> GetRequiredExtensions();
    public SurfaceKHR CreateSurface(Instance instance);
}
