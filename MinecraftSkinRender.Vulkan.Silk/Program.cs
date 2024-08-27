using System.Text;
using MinecraftSkinRender.MojangApi;
using Newtonsoft.Json;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;

namespace MinecraftSkinRender.Vulkan.Silk;

internal class Program
{
    static async Task Main(string[] args)
    {
        bool havecape = true;

        //Console.WriteLine("Download skin");

        //var res = await MinecraftAPI.GetMinecraftProfileNameAsync("Color_yr");
        //var res1 = await MinecraftAPI.GetUserProfile(res!.UUID);
        //TexturesObj? obj = null;
        //foreach (var item in res1!.properties)
        //{
        //    if (item.name == "textures")
        //    {
        //        var temp = Convert.FromBase64String(item.value);
        //        var data = Encoding.UTF8.GetString(temp);
        //        obj = JsonConvert.DeserializeObject<TexturesObj>(data);
        //        break;
        //    }
        //}
        //if (obj == null)
        //{
        //    Console.WriteLine("No have skin");
        //    return;
        //}
        //if (obj!.textures.SKIN.url != null)
        //{
        //    var data = await MinecraftAPI.Client.GetByteArrayAsync(obj!.textures.SKIN.url);
        //    File.WriteAllBytes("skin.png", data);
        //}
        //if (obj.textures.CAPE.url != null)
        //{
        //    var data = await MinecraftAPI.Client.GetByteArrayAsync(obj!.textures.CAPE.url);
        //    File.WriteAllBytes("cape.png", data);
        //    havecape = true;
        //}

        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(400, 400),
            Title = "Vulkan",
        };

        var window = Window.Create(options)!;
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }

        var skin = new SkinRenderVulkan(window.VkSurface);

        window.Resize += (size)=> 
        {
            skin.Width = size.X;
            skin.Height = size.Y;
        };

        window.Render += (time)=> 
        {
            skin.VulkanRender(window.Time);
        };

        skin.Width = window.FramebufferSize.X;
        skin.Height = window.FramebufferSize.Y;
        skin.ChangeSkin(SKBitmap.Decode("skin.png"));
        skin.ChangeType(SkinType.NewSlim);
        skin.ChangeCape(SKBitmap.Decode("cape.png"));
        skin.EnableTop = true;
        skin.EnableMSAA = true;
        skin.VulkanInit();
        window.Run();
    }
}
