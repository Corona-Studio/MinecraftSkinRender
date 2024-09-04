using System.Numerics;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;

namespace MinecraftSkinRender.OpenGL;

public partial class SkinRenderOpenGL(GL gl) : SkinRender
{
    private bool _init = false;

    private uint _textureSkin;
    private uint _textureCape;
    private uint _steveModelDrawOrderCount;

    private uint _shaderProgram;

    private uint _colorRenderBuffer;
    private uint _depthRenderBuffer;
    private uint _frameBuffer;

    private uint _width, _height;

    private readonly ModelVAO _normalVAO = new();
    private readonly ModelVAO _topVAO = new();

    public bool IsGLES { get; set; }

    public unsafe void OpenGlInit()
    {
        if (_init)
            return;

        _init = true;

        CheckError(gl);

        gl.ClearColor(0, 0, 0, 1);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.CullFace(TriangleFace.Back);

        CheckError(gl);

        Info = $"Renderer: {SilkMarshal.PtrToString((nint)gl.GetString(StringName.Renderer))}\n" +
            $"Version: {SilkMarshal.PtrToString((nint)gl.GetString(StringName.Version))} GLSL Version: {SilkMarshal.PtrToString((nint)gl.GetString(StringName.ShadingLanguageVersion))}";

        CreateShader();

        InitVAO(_normalVAO);
        InitVAO(_topVAO);

        _textureSkin = gl.GenTexture();
        _textureCape = gl.GenTexture();

        CheckError(gl);
    }

    private void InitFrameBuffer()
    {
        _colorRenderBuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _colorRenderBuffer);
        gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer,
            8, InternalFormat.Rgba8, _width, _height);

        _depthRenderBuffer = gl.GenRenderbuffer();
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRenderBuffer);
        gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer,
            8, InternalFormat.DepthComponent24, _width, _height);

        _frameBuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);

        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _colorRenderBuffer);

        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthRenderBuffer);

        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        {
            throw new Exception("glCheckFramebufferStatus status != GL_FRAMEBUFFER_COMPLETE");
        }
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
            gl.BindTexture(TextureTarget.Texture2D, _textureCape);
            var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");
            var mat = GetMatrix4(MatrPartType.Cape);
            gl.UniformMatrix4(modelLoc, 1, false, (float*)&mat);
            gl.BindVertexArray(_normalVAO.Cape.VertexArrayObject);
            gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
                DrawElementsType.UnsignedShort, null);

            gl.BindTexture(TextureTarget.Texture2D, 0);
        }
    }

    private unsafe void DrawSkin()
    {
        gl.BindTexture(TextureTarget.Texture2D, _textureSkin);

        var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");
        var modelMat = Matrix4x4.Identity;
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.Body.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.Head);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.Head.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.LeftArm);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.LeftArm.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.RightArm);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.RightArm.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.LeftLeg);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.LeftLeg.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.RightLeg);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.RightLeg.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    private unsafe void DrawSkinTop()
    {
        gl.BindTexture(TextureTarget.Texture2D, _textureSkin);

        var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");
        var modelMat = GetMatrix4(MatrPartType.Body);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.Body.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.Head);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.Head.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.LeftArm);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.LeftArm.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.RightArm);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.RightArm.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.LeftLeg);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.LeftLeg.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = GetMatrix4(MatrPartType.RightLeg);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.RightLeg.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        gl.BindTexture(TextureTarget.Texture2D, 0);
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
            _width = (uint)Width;
            _height = (uint)Height;
            DeleteFrameBuffer();
            InitFrameBuffer();
        }

        if (_width == 0 || _height == 0)
        {
            return;
        }

        if (EnableMSAA)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);
        }

        gl.Viewport(0, 0, _width, _height);
     
        gl.ClearColor(BackColor.X, BackColor.Y, BackColor.Z, BackColor.W);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        CheckError(gl);
        gl.Enable(EnableCap.CullFace);
        gl.Enable(EnableCap.DepthTest);
        gl.DepthMask(true);
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.UseProgram(_shaderProgram);

        //if (IsGLES)
        //{
        //    gl.ClearDepth(1);
        //    gl.DepthMask(true);
        //    gl.Disable(EnableCap.CullFace);
        //    gl.Disable(EnableCap.ScissorTest);
        //    gl.DepthFunc(DepthFunction.Less);
        //}

        CheckError(gl);

        var viewLoc = gl.GetUniformLocation(_shaderProgram, "view");
        var projectionLoc = gl.GetUniformLocation(_shaderProgram, "projection");
        var modelLoc = gl.GetUniformLocation(_shaderProgram, "model");

        var matr = GetMatrix4(MatrPartType.Proj);
        gl.UniformMatrix4(projectionLoc, 1, false, (float*)&matr);

        matr = GetMatrix4(MatrPartType.View);
        gl.UniformMatrix4(viewLoc, 1, false, (float*)&matr);

        matr = GetMatrix4(MatrPartType.Model);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&matr);
        
        CheckError(gl);

        DrawSkin();
        DrawCape();

        if (EnableTop)
        {
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.DepthMask(false);

            DrawSkinTop();

            gl.Disable(EnableCap.Blend);
            gl.DepthMask(true);
        }

        if (EnableMSAA)
        {
            gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, (uint)fb);
            gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _frameBuffer);
            gl.BlitFramebuffer(0, 0, (int)_width, (int)_height, 0, 0, (int)_width,
                (int)_height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        CheckError(gl);
    }

    private static void CheckError(GL gl)
    {
        GLEnum err;
        while ((err = gl.GetError()) != GLEnum.NoError)
            Console.WriteLine(err);
    }

    public unsafe void OpenGlDeinit()
    {
        _skina.Close();

        // Unbind everything
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
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