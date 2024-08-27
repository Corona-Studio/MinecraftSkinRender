using System.Net;
using System.Text;
using MinecraftSkinRender.MojangApi;
using Newtonsoft.Json;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace MinecraftSkinRender.OpenGL.Silk;

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

        // Create a Silk.NET window as usual
        using var window = Window.Create(WindowOptions.Default with
        { 
            Size = new(400, 400)
        });

        // Declare some variables
        GL gl = null;
        var skin = new SkinRenderOpenGL();
        skin.ChangeSkin(SKBitmap.Decode("skin.png"));
        skin.ChangeType(SkinType.NewSlim);
        skin.EnableTop = true;
        skin.EnableMSAA = true;
        skin.SetAnimation(true);
        if (havecape)
        {
            skin.ChangeCape(SKBitmap.Decode("cape.png"));
        }

        // Our loading function
        window.Load += () =>
        {
            gl = window.CreateOpenGL();
            skin.OpenGlInit(gl);
            skin.Width = window.FramebufferSize.X;
            skin.Height = window.FramebufferSize.Y;
        };

        // Handle resizes
        window.FramebufferResize += s =>
        {
            skin.Width = s.X;
            skin.Height = s.Y;
        };

        // The render function
        window.Render += delta =>
        {
            skin.OpenGlRender(gl, 0, delta);
            //gl.Clear(ClearBufferMask.ColorBufferBit);
            //gl.ClearColor(0, 0, 1, 0);
            //window.SwapBuffers();
        };

        // The closing function
        window.Closing += () =>
        {
            skin.OpenGlDeinit(gl);
            // Unload OpenGL
            gl?.Dispose();
        };

        // Now that everything's defined, let's run this bad boy!
        window.Run();

        window.Dispose();
    }
}
