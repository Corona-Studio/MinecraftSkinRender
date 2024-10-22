using System.Numerics;

namespace MinecraftSkinRender.OpenGL;

public partial class SkinRenderOpenGL(OpenGLApi gl) : SkinRender
{
    private bool _init = false;

    private int _textureSkin;
    private int _textureCape;
    private int _steveModelDrawOrderCount;

    private int _shaderProgram;

    private int _colorRenderBuffer;
    private int _depthRenderBuffer;
    private int _frameBuffer;

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

        _textureSkin = gl.GenTexture();
        _textureCape = gl.GenTexture();

        CheckError();
    }

    private void InitFrameBuffer()
    {
        _colorRenderBuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(gl.GL_RENDERBUFFER, _colorRenderBuffer);
        gl.RenderbufferStorageMultisample(gl.GL_RENDERBUFFER,
            8, gl.GL_RGBA8, _width, _height);

        _depthRenderBuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(gl.GL_RENDERBUFFER, _depthRenderBuffer);
        gl.RenderbufferStorageMultisample(gl.GL_RENDERBUFFER,
            8, gl.GL_DEPTH_COMPONENT24, _width, _height);

        _frameBuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(gl.GL_FRAMEBUFFER, _frameBuffer);

        gl.FramebufferRenderbuffer(gl.GL_FRAMEBUFFER,
            gl.GL_COLOR_ATTACHMENT0, gl.GL_RENDERBUFFER, _colorRenderBuffer);

        gl.FramebufferRenderbuffer(gl.GL_FRAMEBUFFER,
            gl.GL_DEPTH_ATTACHMENT, gl.GL_RENDERBUFFER, _depthRenderBuffer);

        if (gl.CheckFramebufferStatus(gl.GL_FRAMEBUFFER) != gl.GL_FRAMEBUFFER_COMPLETE)
        {
            throw new Exception("glCheckFramebufferStatus status != GL_FRAMEBUFFER_COMPLETE");
        }
        gl.BindRenderbuffer(gl.GL_RENDERBUFFER, 0);
        gl.BindFramebuffer(gl.GL_FRAMEBUFFER, 0);
    }

    private void DeleteFrameBuffer()
    {
        if (_frameBuffer != 0)
        {
            gl.DeleteFramebuffer(_frameBuffer);
            _frameBuffer = 0;
        }

        if (_colorRenderBuffer != 0)
        {
            gl.DeleteRenderbuffer(_colorRenderBuffer);
            _colorRenderBuffer = 0;
        }

        if (_depthRenderBuffer != 0)
        {
            gl.DeleteRenderbuffer(_depthRenderBuffer);
            _depthRenderBuffer = 0;
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

        if (EnableMSAA)
        {
            gl.BindFramebuffer(gl.GL_FRAMEBUFFER, _frameBuffer);
        }

        gl.Viewport(0, 0, _width, _height);
     
        gl.ClearColor(BackColor.X, BackColor.Y, BackColor.Z, BackColor.W);
        gl.Clear(gl.GL_COLOR_BUFFER_BIT | gl.GL_DEPTH_BUFFER_BIT);

        CheckError();
        gl.Enable(gl.GL_CULL_FACE);
        gl.Enable(gl.GL_DEPTH_TEST);
        gl.DepthMask(true);
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

        DrawSkin();
        DrawCape();

        if (EnableTop)
        {
            gl.Enable(gl.GL_BLEND);
            gl.BlendFunc(gl.GL_SRC_ALPHA, gl.GL_ONE_MINUS_SRC_ALPHA);
            gl.DepthMask(false);

            DrawSkinTop();

            gl.Disable(gl.GL_BLEND);
            gl.DepthMask(true);
        }

        if (EnableMSAA)
        {
            gl.BindFramebuffer(gl.GL_DRAW_FRAMEBUFFER, fb);
            gl.BindFramebuffer(gl.GL_READ_FRAMEBUFFER, _frameBuffer);
            gl.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width,
                _height, gl.GL_COLOR_BUFFER_BIT, gl.GL_NEAREST);
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

        DeleteTexture();

        gl.DeleteProgram(_shaderProgram);

        DeleteFrameBuffer();
    }
}