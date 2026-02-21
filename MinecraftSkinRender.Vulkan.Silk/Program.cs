using MinecraftSkinRender.Vulkan.KHR;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SkiaSharp;

namespace MinecraftSkinRender.Vulkan.Silk;

internal class Program
{
    private static IWindow _window;

    static async Task Main(string[] args)
    {
        bool havecape = true;

        if (OperatingSystem.IsMacOS())
        {
            Environment.SetEnvironmentVariable("DYLD_LIBRARY_PATH", "~/VulkanSDK/1.4.341.0/macOS/lib:" + Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH"));
            Environment.SetEnvironmentVariable("VK_ICD_FILENAMES", "~/VulkanSDK/1.4.341.0/macOS/share/vulkan/icd.d/MoltenVK_icd.json");
        }

        await SkinDownloader.Download();

        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(400, 400),
            Title = "Vulkan",
            FramesPerSecond = 60
        };

        _window = Window.Create(options)!;
        _window.Initialize();

        if (_window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        var skin = new SkinRenderVulkanKHR(Vk.GetApi(), new VKHandel());

        _window.Resize += (size) =>
        {
            skin.Width = size.X;
            skin.Height = size.Y;
        };

        _window.Render += (time) =>
        {
            skin.Rot(0, 1f);
            skin.Tick(time);
            skin.VulkanRender();
        };

        skin.Width = _window.FramebufferSize.X;
        skin.Height = _window.FramebufferSize.Y;
        skin.BackColor = new(0, 1, 0, 1);
        var img = SKBitmap.Decode("skin.png");
        skin.SetSkinTex(img);
        skin.SkinType = SkinType.NewSlim;
        skin.SetCapeTex(SKBitmap.Decode("cape.png"));
        skin.EnableTop = true;
        skin.EnableCape = havecape;
        skin.Animation = true;
        skin.FpsUpdate += (a, b) =>
        {
            Console.WriteLine("Fps: " + b);
        };
        skin.VulkanInit();

        _ = Task.Run(() =>
        {
            while (true)
            {
                Thread.Sleep(2000);
                //skin.SetSkin(img);
            }
        });

        _window.Run();
    }

    class VKHandel : IVulkanApi
    {
        public unsafe IReadOnlyList<string> GetRequiredExtensions()
        {
            var glfwExtensions = _window.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
            var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

            foreach (var item in extensions)
            {
                Console.WriteLine(item);
            }

            return extensions;
        }

        public unsafe SurfaceKHR CreateSurface(Instance instance)
        {
            return _window.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
        }
    }
}
