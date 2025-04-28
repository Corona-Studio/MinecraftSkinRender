using System.Numerics;

namespace MinecraftSkinRender.OpenGL;

public partial class OpenGLFXAA(OpenGLApi gl) : SkinRender
{
    private bool _init = false;

    private int _textureSkin;
    private int _textureCape;
    private int _steveModelDrawOrderCount;

    private int _shaderProgram;
    private int _shaderFXAAProgram;

    private int _msaaRenderBuffer;
    private int _msaaRenderTexture;
    private int _msaaFrameBuffer;

    private int _fxaaRenderBuffer;
    private int _fxaaTexture;
    private int _fxaaFrameBuffer;

    private int _width, _height;

    private readonly ModelVAO _normalVAO = new();
    private readonly ModelVAO _topVAO = new();

    public bool IsGLES { get; set; }

    public unsafe void OpenGlInit()
    {
        if (_init)
            return;

        _init = true;

        CheckError();

        gl.ClearColor(0, 0, 0, 1);
        gl.BlendFunc(gl.GL_SRC_ALPHA, gl.GL_ONE_MINUS_SRC_ALPHA);
        gl.CullFace(gl.GL_BACK);

        CheckError();

        Info = $"Renderer: {gl.GetString(gl.GL_RENDERER)}\n" +
            $"OpenGL Version: {gl.GetString(gl.GL_VERSION)}\n" +
            $"GLSL Version: {gl.GetString(gl.GL_SHADING_LANGUAGE_VERSION)}";

        CreateShader();

        InitVAO(_normalVAO);
        InitVAO(_topVAO);

        InitFXAA();

        _textureSkin = gl.GenTexture();
        _textureCape = gl.GenTexture();

        CheckError();
    }

    private void InitFrameBuffer()
    {
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

        {
            _fxaaFrameBuffer = gl.GenFramebuffer();
            gl.BindFramebuffer(gl.GL_FRAMEBUFFER, _fxaaFrameBuffer);

            _fxaaTexture = gl.GenTexture();
            gl.BindTexture(gl.GL_TEXTURE_2D, _fxaaTexture);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_WRAP_S, gl.GL_CLAMP_TO_EDGE);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_WRAP_T, gl.GL_CLAMP_TO_EDGE);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_MIN_FILTER, gl.GL_LINEAR);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_MAG_FILTER, gl.GL_LINEAR);
            gl.TexImage2D(gl.GL_TEXTURE_2D, 0, gl.GL_RGBA, _width, _height, 0, gl.GL_RGBA, gl.GL_UNSIGNED_BYTE, 0);
            gl.FramebufferTexture2D(gl.GL_FRAMEBUFFER, gl.GL_COLOR_ATTACHMENT0, gl.GL_TEXTURE_2D, _fxaaTexture, 0);

            _fxaaRenderBuffer = gl.GenRenderbuffer();
            gl.BindRenderbuffer(gl.GL_RENDERBUFFER, _fxaaRenderBuffer);
            gl.RenderbufferStorage(gl.GL_RENDERBUFFER,
                gl.GL_DEPTH24_STENCIL8, _width, _height);
            gl.FramebufferRenderbuffer(gl.GL_FRAMEBUFFER, gl.GL_DEPTH_STENCIL_ATTACHMENT,
                gl.GL_RENDERBUFFER, _fxaaRenderBuffer);

            if (gl.CheckFramebufferStatus(gl.GL_FRAMEBUFFER) != gl.GL_FRAMEBUFFER_COMPLETE)
            {
                throw new Exception("glCheckFramebufferStatus status != GL_FRAMEBUFFER_COMPLETE");
            }

            gl.BindTexture(gl.GL_TEXTURE_2D, 0);
            gl.BindRenderbuffer(gl.GL_RENDERBUFFER, 0);
            gl.BindFramebuffer(gl.GL_FRAMEBUFFER, 0);
        }
    }

    private void DeleteFrameBuffer()
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

        if (_fxaaFrameBuffer != 0)
        {
            gl.DeleteFramebuffer(_fxaaFrameBuffer);
            _fxaaFrameBuffer = 0;
        }
        if (_fxaaRenderBuffer != 0)
        {
            gl.DeleteRenderbuffer(_fxaaRenderBuffer);
            _fxaaRenderBuffer = 0;
        }
        if (_fxaaTexture != 0)
        {
            gl.DeleteTexture(_fxaaTexture);
            _fxaaTexture = 0;
        }
    }

    private unsafe void DrawCape()
    {
        if (HaveCape && EnableCape)
        {
            gl.BindTexture(gl.GL_TEXTURE_2D, _textureCape);
            var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");
            var mat = GetMatrix4(MatrPartType.Cape);
            gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&mat);
            gl.BindVertexArray(_normalVAO.Cape.VertexArrayObject);
            gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
                gl.GL_UNSIGNED_SHORT, 0);

            gl.BindTexture(gl.GL_TEXTURE_2D, 0);
        }
    }

    private unsafe void DrawSkin()
    {
        gl.BindTexture(gl.GL_TEXTURE_2D, _textureSkin);

        var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");
        var modelMat = Matrix4x4.Identity;
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.Body.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.Head);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.Head.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.LeftArm);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.LeftArm.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.RightArm);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.RightArm.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.LeftLeg);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.LeftLeg.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.RightLeg);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.RightLeg.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        gl.BindTexture(gl.GL_TEXTURE_2D, 0);
    }

    private unsafe void DrawSkinTop()
    {
        gl.BindTexture(gl.GL_TEXTURE_2D, _textureSkin);

        var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");
        var modelMat = GetMatrix4(MatrPartType.Body);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.Body.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.Head);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.Head.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.LeftArm);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.LeftArm.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.RightArm);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.RightArm.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.LeftLeg);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.LeftLeg.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        modelMat = GetMatrix4(MatrPartType.RightLeg);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.RightLeg.VertexArrayObject);
        gl.DrawElements(gl.GL_TRIANGLES, _steveModelDrawOrderCount,
           gl.GL_UNSIGNED_SHORT, 0);

        gl.BindTexture(gl.GL_TEXTURE_2D, 0);
    }

    public unsafe void OpenGlRender(int fb)
    {
        if (_switchSkin)
        {
            LoadSkin();
        }
        if (_switchModel)
        {
            LoadModel();
        }

        if (!HaveSkin)
        {
            return;
        }

        if (Width == 0 || Height == 0)
        {
            return;
        }

        if (Width != _width || Height != _height)
        {
            _width = Width;
            _height = Height;
            DeleteFrameBuffer();
            InitFrameBuffer();
        }

        if (_width == 0 || _height == 0)
        {
            return;
        }

        if (RenderType == SkinRenderType.MSAA)
        {
            gl.BindFramebuffer(gl.GL_FRAMEBUFFER, _msaaFrameBuffer);
        }
        else if (RenderType == SkinRenderType.FXAA)
        {
            gl.BindFramebuffer(gl.GL_FRAMEBUFFER, _fxaaFrameBuffer);
        }

        gl.Viewport(0, 0, _width, _height);
     
        gl.ClearColor(BackColor.X, BackColor.Y, BackColor.Z, BackColor.W);
        gl.ClearDepth(1.0f);
        gl.Clear(gl.GL_COLOR_BUFFER_BIT | gl.GL_DEPTH_BUFFER_BIT);

        CheckError();

        gl.Enable(gl.GL_CULL_FACE);
        gl.Enable(gl.GL_DEPTH_TEST);
        gl.ActiveTexture(gl.GL_TEXTURE0);
        gl.UseProgram(_shaderProgram);

        //if (IsGLES)
        //{
        //    gl.ClearDepth(1);
        //    gl.DepthMask(true);
        //    gl.Disable(gl.GL_CULL_FACE);
        //    gl.Disable(EnableCap.ScissorTest);
        //    gl.DepthFunc(DepthFunction.Less);
        //}

        CheckError();

        var viewLoc = gl.GetUniformLocation(_shaderProgram, "view");
        var projectionLoc = gl.GetUniformLocation(_shaderProgram, "projection");
        var modelLoc = gl.GetUniformLocation(_shaderProgram, "model");

        var matr = GetMatrix4(MatrPartType.Proj);
        gl.UniformMatrix4fv(projectionLoc, 1, false, (float*)&matr);

        matr = GetMatrix4(MatrPartType.View);
        gl.UniformMatrix4fv(viewLoc, 1, false, (float*)&matr);

        matr = GetMatrix4(MatrPartType.Model);
        gl.UniformMatrix4fv(modelLoc, 1, false, (float*)&matr);
        
        CheckError();

        gl.DepthMask(true);
        gl.Disable(gl.GL_BLEND);

        DrawSkin();
        DrawCape();

        if (EnableTop)
        {
            gl.DepthMask(false);
            gl.Enable(gl.GL_BLEND);
            gl.Enable(gl.GL_SAMPLE_ALPHA_TO_COVERAGE);
            gl.BlendFunc(gl.GL_SRC_ALPHA, gl.GL_ONE_MINUS_SRC_ALPHA);

            DrawSkinTop();

            gl.DepthMask(true);
            gl.Disable(gl.GL_BLEND);
        }

        if (RenderType == SkinRenderType.MSAA)
        {
            gl.BindFramebuffer(gl.GL_DRAW_FRAMEBUFFER, fb);
            gl.BindFramebuffer(gl.GL_READ_FRAMEBUFFER, _msaaFrameBuffer);
            gl.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width,
                _height, gl.GL_COLOR_BUFFER_BIT, gl.GL_NEAREST);
            gl.BindFramebuffer(gl.GL_FRAMEBUFFER, 0);
        }
        else if (RenderType == SkinRenderType.FXAA)
        {
            gl.Disable(gl.GL_DEPTH_TEST);
            gl.BindFramebuffer(gl.GL_FRAMEBUFFER, fb);
            gl.Viewport(0, 0, _width, _height);
            gl.Clear(gl.GL_COLOR_BUFFER_BIT);
            gl.UseProgram(_shaderFXAAProgram);

            var g_texelStepLocation = gl.GetUniformLocation(_shaderFXAAProgram, "u_texelStep");
            var g_showEdgesLocation = gl.GetUniformLocation(_shaderFXAAProgram, "u_showEdges");
            var g_fxaaOnLocation = gl.GetUniformLocation(_shaderFXAAProgram, "u_fxaaOn");

            var g_lumaThresholdLocation = gl.GetUniformLocation(_shaderFXAAProgram, "u_lumaThreshold");
            var g_mulReduceLocation = gl.GetUniformLocation(_shaderFXAAProgram, "u_mulReduce");
            var g_minReduceLocation = gl.GetUniformLocation(_shaderFXAAProgram, "u_minReduce");
            var g_maxSpanLocation = gl.GetUniformLocation(_shaderFXAAProgram, "u_maxSpan");

            gl.Uniform1i(g_showEdgesLocation, 0);
            gl.Uniform1i(g_fxaaOnLocation, 1);

            gl.Uniform1f(g_lumaThresholdLocation, 0.5f);
            gl.Uniform1f(g_mulReduceLocation, 1.0f / 8.0f);
            gl.Uniform1f(g_minReduceLocation, 1.0f / 128.0f);
            gl.Uniform1f(g_maxSpanLocation, 8.0f);
            gl.Uniform2f(g_texelStepLocation, 1.0f / _width, 1.0f / _height);
            gl.ActiveTexture(gl.GL_TEXTURE0);
            gl.BindTexture(gl.GL_TEXTURE_2D, _fxaaTexture);
            gl.BindVertexArray(_fxaaVAO);
            gl.DrawArrays(gl.GL_TRIANGLE_STRIP, 0, 4);
            gl.BindVertexArray(0);
            gl.Enable(gl.GL_DEPTH_TEST);
            gl.BindTexture(gl.GL_TEXTURE_2D, 0);
            gl.BindFramebuffer(gl.GL_FRAMEBUFFER, 0);
        }

        CheckError();
    }

    private void CheckError()
    {
        int err;
        while ((err = gl.GetError()) != 0)
            Console.WriteLine(err);
    }

    public unsafe void OpenGlDeinit()
    {
        _skina.Close();

        // Unbind everything
        gl.BindBuffer(gl.GL_ARRAY_BUFFER, 0);
        gl.BindBuffer(gl.GL_ELEMENT_ARRAY_BUFFER, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        // Delete all resources.
        DeleteVAO(_normalVAO);
        DeleteVAO(_topVAO);

        DeleteFrameBuffer();

        DeleteTexture();

        gl.DeleteProgram(_shaderProgram);
        gl.DeleteProgram(_shaderFXAAProgram);
    }
}