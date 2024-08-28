using Silk.NET.OpenGL;
using SkiaSharp;

namespace MinecraftSkinRender.OpenGL;

public partial class SkinRenderOpenGL
{
    private unsafe void LoadTex(SKBitmap image, uint tex)
    {
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, tex);

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);

        var format = PixelFormat.Rgba;
        if (image.ColorType == SKColorType.Bgra8888)
        {
            format = PixelFormat.Bgra;
        }

        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)image.Width,
               (uint)image.Height, 0, format, PixelType.UnsignedByte, (void*)image.GetPixels());
        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    private void LoadSkin()
    {
        if (Skin == null)
        {
            OnErrorChange(ErrorType.SkinNotFind);
            return;
        }

        if (SkinType == SkinType.Unkonw)
        {
            OnErrorChange(ErrorType.UnknowSkinType);
            return;
        }

        LoadTex(Skin, _textureSkin);

        if (Cape != null)
        {
            LoadTex(Cape, _textureCape);
        }

        CheckError(gl);

        _switchSkin = false;
    }

    private void DeleteTexture()
    {
        if (_textureSkin != 0)
        {
            gl.DeleteTexture(_textureSkin);
            _textureSkin = 0;
        }
        if (_textureCape != 0)
        {
            gl.DeleteTexture(_textureCape);
            _textureCape = 0;
        }
    }
}
