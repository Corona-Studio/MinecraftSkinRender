using System.Text;
using System.Text.Json;
using MinecraftSkinRender.MojangApi;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace MinecraftSkinRender.OpenGL.Silk;

internal class Program
{
    static async Task Main(string[] args)
    {
        bool havecape = true;

        await SkinDownloader.Download();

        // Create a Silk.NET window as usual
        using var window = Window.Create(WindowOptions.Default with
        {
            API = GraphicsAPI.Default with
            {
                // API = ContextAPI.OpenGL,
                // Version = new(3, 0)
            },
            Size = new(400, 400),
            VSync = true
        });

        // Declare some variables
        GL gl = null;
        SkinRenderOpenGL? skin = null;

        // Our loading function
        window.Load += () =>
        {
            gl = window.CreateOpenGL();
            skin = new(new SlikOpenglApi(gl))
            {
                IsGLES = true
            };
            var img = SKBitmap.Decode("skin.png");
            skin.SetSkinTex(img);
            skin.SkinType = SkinType.NewSlim;
            skin.EnableTop = true;
            skin.RenderType = SkinRenderType.Normal;
            skin.Animation = true;
            skin.EnableCape = true;
            if (havecape)
            {
                skin.SetCapeTex(SKBitmap.Decode("cape.png"));
            }
            skin.FpsUpdate += (a, b) =>
            {
                Console.WriteLine("Fps: " + b);
            };
            skin.BackColor = new(1, 1, 1, 1);
            skin.Width = window.FramebufferSize.X;
            skin.Height = window.FramebufferSize.Y;
            skin.OpenGlInit();
        };

        // Handle resizes
        window.FramebufferResize += s =>
        {
            if (skin == null)
            {
                return;
            }
            skin.Width = s.X;
            skin.Height = s.Y;
        };

        // The render function
        window.Render += delta =>
        {
            if (skin == null)
            {
                return;
            }
            skin.Rot(0, 1f);
            skin.Tick(delta);
            skin.OpenGlRender(0);
            //gl.Clear(ClearBufferMask.ColorBufferBit);
            //gl.ClearColor(0, 0, 1, 0);
            //window.SwapBuffers();
        };

        // The closing function
        window.Closing += () =>
        {
            if (skin == null)
            {
                return;
            }
            skin.OpenGlDeinit();
            // Unload OpenGL
            gl?.Dispose();
        };

        // Now that everything's defined, let's run this bad boy!
        window.Run();

        window.Dispose();
    }
}
