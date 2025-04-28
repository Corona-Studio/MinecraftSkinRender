using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftSkinRender.OpenGL;

public partial class SkinRenderOpenGL
{
    private int _msaaRenderBuffer;
    private int _msaaRenderTexture;
    private int _msaaFrameBuffer;

    private void InitMSAAFrameBuffer()
    {
        _msaaFrameBuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(gl.GL_FRAMEBUFFER, _msaaFrameBuffer);

        _msaaRenderTexture = gl.GenTexture();
        gl.BindTexture(gl.GL_TEXTURE_2D_MULTISAMPLE, _msaaRenderTexture);
        gl.TexImage2DMultisample(gl.GL_TEXTURE_2D_MULTISAMPLE, 4,
            gl.GL_RGBA8, _width, _height, true);
        gl.FramebufferTexture2D(gl.GL_FRAMEBUFFER, gl.GL_COLOR_ATTACHMENT0,
            gl.GL_TEXTURE_2D_MULTISAMPLE, _msaaRenderTexture, 0);

        _msaaRenderBuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(gl.GL_RENDERBUFFER, _msaaRenderBuffer);
        gl.RenderbufferStorageMultisample(gl.GL_RENDERBUFFER, 4,
            gl.GL_DEPTH24_STENCIL8, _width, _height);
        gl.FramebufferRenderbuffer(gl.GL_FRAMEBUFFER, gl.GL_DEPTH_STENCIL_ATTACHMENT,
            gl.GL_RENDERBUFFER, _msaaRenderBuffer);

        if (gl.CheckFramebufferStatus(gl.GL_FRAMEBUFFER) != gl.GL_FRAMEBUFFER_COMPLETE)
        {
            throw new Exception("glCheckFramebufferStatus status != GL_FRAMEBUFFER_COMPLETE");
        }
        gl.BindTexture(gl.GL_TEXTURE_2D_MULTISAMPLE, 0);
        gl.BindRenderbuffer(gl.GL_RENDERBUFFER, 0);
        gl.BindFramebuffer(gl.GL_FRAMEBUFFER, 0);
    }

    private void DeleteMSAAFrameBuffer()
    {
        if (_msaaFrameBuffer != 0)
        {
            gl.DeleteFramebuffer(_msaaFrameBuffer);
            _msaaFrameBuffer = 0;
        }
        if (_msaaRenderBuffer != 0)
        {
            gl.DeleteRenderbuffer(_msaaRenderBuffer);
            _msaaRenderBuffer = 0;
        }
        if (_msaaRenderTexture != 0)
        {
            gl.DeleteTexture(_msaaRenderTexture);
            _msaaRenderTexture = 0;
        }
    }
}
