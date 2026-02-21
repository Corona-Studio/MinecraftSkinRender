using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR; // 添加这个命名空间

namespace MinecraftSkinRender.Vulkan.KHR;

public partial class SkinRenderVulkanKHR
{
    private unsafe string[] GetRequiredExtensions()
    {
        // 获取基础扩展
        var baseExtensions = ivk.GetRequiredExtensions().ToList();

        if (!baseExtensions.Contains("VK_KHR_portability_enumeration"))
        {
            baseExtensions.Add("VK_KHR_portability_enumeration");
        }

        // 添加 macOS 表面扩展（如果需要）
        // 检查是否已经有表面扩展，如果没有则添加
        bool hasSurfaceExt = baseExtensions.Any(ext =>
            ext == KhrSurface.ExtensionName ||
            ext == "VK_MVK_macos_surface" ||
            ext == "VK_EXT_metal_surface");

        if (!hasSurfaceExt)
        {
            // 优先使用 Metal 表面扩展（较新）
            baseExtensions.Add("VK_EXT_metal_surface");
        }

#if DEBUG
        if (EnableValidationLayers)
        {
            if (!baseExtensions.Contains(ExtDebugUtils.ExtensionName))
            {
                baseExtensions.Add(ExtDebugUtils.ExtensionName);
            }
        }
#endif

        return baseExtensions.ToArray();
    }

    private unsafe void CreateInstance()
    {
        if (EnableValidationLayers && !CheckValidationLayerSupport())
        {
            throw new Exception("validation layers requested, but not available!");
        }

        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Minecraft Skin Render"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version13 // 建议使用 Vulkan 1.3 以获得更好的兼容性
        };

        // 获取扩展列表
        var extensions = GetRequiredExtensions();

        // 打印扩展信息（用于调试）
        Console.WriteLine("Requesting instance extensions:");
        foreach (var ext in extensions)
        {
            Console.WriteLine($"  - {ext}");
        }

        // 创建实例信息，关键部分是设置 Flags
        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            // 关键：设置可移植性枚举标志
            Flags = InstanceCreateFlags.EnumeratePortabilityBitKhr
        };

        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
        createInfo.EnabledLayerCount = 0;
        createInfo.PNext = null;

#if DEBUG
        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
#endif

        // 创建实例
        fixed (Instance* instancePtr = &_instance)
        {
            if (vk.CreateInstance(ref createInfo, null, instancePtr) != Result.Success)
            {
                // 如果失败，尝试不带标志重新创建（用于调试）
                Console.WriteLine("Failed to create instance with portability flag, trying without...");
                createInfo.Flags = 0;

                if (vk.CreateInstance(ref createInfo, null, instancePtr) != Result.Success)
                {
                    throw new Exception("failed to create instance!");
                }
            }
        }

        // 清理内存
        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

#if DEBUG
        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
#endif

        Console.WriteLine("Vulkan instance created successfully!");
    }

    // 添加一个方法来检查可用的扩展（可选，用于调试）
    // private unsafe void PrintAvailableExtensions()
    // {
    //     uint extensionCount = 0;
    //     vk.EnumerateInstanceExtensionProperties((byte*)null, ref extensionCount, null);
    //     
    //     if (extensionCount > 0)
    //     {
    //         var extensions = new ExtensionProperties[extensionCount];
    //         fixed (ExtensionProperties* extensionsPtr = extensions)
    //         {
    //             vk.EnumerateInstanceExtensionProperties((byte*)null, ref extensionCount, extensionsPtr);
    //         }
    //         
    //         Console.WriteLine("Available instance extensions:");
    //         for (int i = 0; i < extensionCount; i++)
    //         {
    //             string extName = SilkMarshal.PtrToString((IntPtr)extensions[i].ExtensionName);
    //             Console.WriteLine($"  - {extName}");
    //         }
    //     }
    // }
}