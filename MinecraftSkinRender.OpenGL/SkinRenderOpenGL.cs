using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace MinecraftSkinRender.OpenGL;

public class SkinRenderOpenGL : SkinRender
{
    private bool _init = false;

    private uint _texture;
    private uint _texture1;
    private uint _steveModelDrawOrderCount;

    private uint _shaderProgram;

    private uint _colorRenderBuffer;
    private uint _depthRenderBuffer;
    private uint _frameBuffer;

    private uint _width, _height;

    private readonly ModelVAO _normalVAO = new();
    private readonly ModelVAO _topVAO = new();
    
    public unsafe void OpenGlInit(GL gl)
    {
        if (_init)
            return;

        _init = true;

        CheckError(gl);

        gl.ClearColor(0, 0, 0, 1);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.CullFace(TriangleFace.Back);

        CheckError(gl);

        Info = $"Renderer: {SilkMarshal.PtrToString((nint)gl.GetString(StringName.Renderer))} Version: {SilkMarshal.PtrToString((nint)gl.GetString(StringName.Version))}";
        int maj = gl.GetInteger(GetPName.MajorVersion);
        int min = gl.GetInteger(GetPName.MinorVersion);

        var vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, SkinShaderOpenGL.VertexShader(new(maj, min), IsGLES, false));
        gl.CompileShader(vertexShader);
        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var state);
        if (state == 0)
        {
            gl.GetShaderInfoLog(vertexShader, out var info);
            throw new Exception($"GL_VERTEX_SHADER.\n{info}");
        }

        var fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, SkinShaderOpenGL.VertexShader(new(maj, min), IsGLES, true));
        gl.CompileShader(fragmentShader);
        state = gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus);
        if (state == 0)
        {
            gl.GetShaderInfoLog(vertexShader, out var info);
            throw new Exception($"GL_FRAGMENT_SHADER.\n{info}");
        }

        _shaderProgram = gl.CreateProgram();
        gl.AttachShader(_shaderProgram, vertexShader);
        gl.AttachShader(_shaderProgram, fragmentShader);
        gl.LinkProgram(_shaderProgram);
        state = gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus);
        if (state == 0)
        {
            gl.GetProgramInfoLog(vertexShader, out var info);
            throw new Exception($"GL_LINK_PROGRAM.\n{info}");
        }

        //Delete the no longer useful individual shaders;
        gl.DetachShader(_shaderProgram, vertexShader);
        gl.DetachShader(_shaderProgram, fragmentShader);
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);

        InitVAO(gl, _normalVAO);
        InitVAO(gl, _topVAO);

        _texture = gl.GenTexture();
        _texture1 = gl.GenTexture();

        CheckError(gl);
    }

    private void InitFrameBuffer(GL gl)
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

        //_frameBuffer = gl.GenFramebuffer();
        //gl.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);

        //_colorRenderBuffer = gl.GenTexture();

        //gl.BindTexture(GLEnum.Texture2DMultisample, _colorRenderBuffer);
        //gl.TexImage2DMultisample(GLEnum.Texture2DMultisample, 4, GLEnum.Rgb, _width, _height, true);
        //gl.BindTexture(GLEnum.Texture2DMultisample, 0);
        //gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2DMultisample, _colorRenderBuffer, 0);

        //var rbo = gl.GenRenderbuffer();
        //gl.BindRenderbuffer(GLEnum.Renderbuffer, rbo);
        //gl.RenderbufferStorageMultisample(GLEnum.Renderbuffer, 4, GLEnum.Depth24Stencil8, _width, _height);
        //gl.BindRenderbuffer(GLEnum.Renderbuffer, 0);
        //gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment, GLEnum.Renderbuffer, rbo);

        //if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
        //{
        //    throw new Exception("glCheckFramebufferStatus status != GL_FRAMEBUFFER_COMPLETE");
        //}
        //gl.BindFramebuffer(GLEnum.Framebuffer, 0);
    }

    private void DeleteFrameBuffer(GL gl)
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

    private static void InitVAOItem(GL gl, VAOItem item)
    {
        item.VertexBufferObject = gl.GenBuffer();
        item.IndexBufferObject = gl.GenBuffer();
    }

    private static void InitVAO(GL gl, ModelVAO vao)
    {
        vao.Head.VertexArrayObject = gl.GenVertexArray();
        vao.Body.VertexArrayObject = gl.GenVertexArray();
        vao.LeftArm.VertexArrayObject = gl.GenVertexArray();
        vao.RightArm.VertexArrayObject = gl.GenVertexArray();
        vao.LeftLeg.VertexArrayObject = gl.GenVertexArray();
        vao.RightLeg.VertexArrayObject = gl.GenVertexArray();
        vao.Cape.VertexArrayObject = gl.GenVertexArray();

        InitVAOItem(gl, vao.Head);
        InitVAOItem(gl, vao.Body);
        InitVAOItem(gl, vao.LeftArm);
        InitVAOItem(gl, vao.RightArm);
        InitVAOItem(gl, vao.LeftLeg);
        InitVAOItem(gl, vao.RightLeg);
        InitVAOItem(gl, vao.Cape);
    }

    private static unsafe void LoadTex(GL gl, SKBitmap image, uint tex)
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

    private void LoadSkin(GL gl)
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

        IsLoading = true;

        LoadTex(gl, Skin, _texture);

        if (Cape != null)
        {
            LoadTex(gl, Cape, _texture1);
        }

        CheckError(gl);

        _switchSkin = false;

        IsLoading = false;
    }

    private unsafe void PutVAO(GL gl, VAOItem vao, CubeModelItemObj model, float[] uv)
    {
        gl.UseProgram(_shaderProgram);
        gl.BindVertexArray(vao.VertexArrayObject);

        uint a_Position = (uint)gl.GetAttribLocation(_shaderProgram, "a_position");
        uint a_texCoord = (uint)gl.GetAttribLocation(_shaderProgram, "a_texCoord");
        uint a_normal = (uint)gl.GetAttribLocation(_shaderProgram, "a_normal");

        gl.DisableVertexAttribArray(a_Position);
        gl.DisableVertexAttribArray(a_texCoord);
        gl.DisableVertexAttribArray(a_normal);

        int size = model.Model.Length / 3;

        var points = new VertexOpenGL[size];

        for (var primitive = 0; primitive < size; primitive++)
        {
            var srci = primitive * 3;
            var srci1 = primitive * 2;
            points[primitive] = new VertexOpenGL
            {
                Position = new(model.Model[srci], model.Model[srci + 1], model.Model[srci + 2]),
                UV = new(uv[srci1], uv[srci1 + 1]),
                Normal = new(CubeModel.Vertices[srci], CubeModel.Vertices[srci + 1], CubeModel.Vertices[srci + 2])
            };
        }

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vao.VertexBufferObject);
        var vertexSize = Marshal.SizeOf<VertexOpenGL>();
        fixed (void* pdata = points)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(points.Length * vertexSize),
                    pdata, BufferUsageARB.StaticDraw);
        }

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, vao.IndexBufferObject);
        fixed (void* pdata = model.Point)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                (nuint)(model.Point.Length * sizeof(ushort)), pdata, BufferUsageARB.StaticDraw);
        }

        gl.VertexAttribPointer(a_Position, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
        gl.VertexAttribPointer(a_texCoord, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
        gl.VertexAttribPointer(a_normal, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 5 * sizeof(float));

        gl.EnableVertexAttribArray(a_Position);
        gl.EnableVertexAttribArray(a_texCoord);
        gl.EnableVertexAttribArray(a_normal);

        gl.BindVertexArray(0);

        CheckError(gl);
    }

    private unsafe void LoadModel(GL gl)
    {
        IsLoading = true;

        var normal = Steve3DModel.GetSteve(SkinType);
        var top = Steve3DModel.GetSteveTop(SkinType);
        var tex = Steve3DTexture.GetSteveTexture(SkinType);
        var textop = Steve3DTexture.GetSteveTextureTop(SkinType);

        _steveModelDrawOrderCount = (uint)normal.Head.Point.Length;

        PutVAO(gl, _normalVAO.Head, normal.Head, tex.Head);
        PutVAO(gl, _normalVAO.Body, normal.Body, tex.Body);
        PutVAO(gl, _normalVAO.LeftArm, normal.LeftArm, tex.LeftArm);
        PutVAO(gl, _normalVAO.RightArm, normal.RightArm, tex.RightArm);
        PutVAO(gl, _normalVAO.LeftLeg, normal.LeftLeg, tex.LeftLeg);
        PutVAO(gl, _normalVAO.RightLeg, normal.RightLeg, tex.RightLeg);
        PutVAO(gl, _normalVAO.Cape, normal.Cape, tex.Cape);

        PutVAO(gl, _topVAO.Head, top.Head, textop.Head);
        if (SkinType != SkinType.Old)
        {
            PutVAO(gl, _topVAO.Head, top.Head, textop.Head);
            PutVAO(gl, _topVAO.Body, top.Body, textop.Body);
            PutVAO(gl, _topVAO.LeftArm, top.LeftArm, textop.LeftArm);
            PutVAO(gl, _topVAO.RightArm, top.RightArm, textop.RightArm);
            PutVAO(gl, _topVAO.LeftLeg, top.LeftLeg, textop.LeftLeg);
            PutVAO(gl, _topVAO.RightLeg, top.RightLeg, textop.RightLeg);
        }
        _switchModel = false;
        IsLoading = false;
    }

    private unsafe void DrawCape(GL gl)
    {
        if (HaveCape && EnableCape)
        {
            gl.BindTexture(TextureTarget.Texture2D, _texture1);

            var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");

            var mat = Matrix4x4.CreateTranslation(0, -2f * CubeModel.Value, -CubeModel.Value * 0.1f) *
               Matrix4x4.CreateRotationX((float)(10.8 * Math.PI / 180)) *
               Matrix4x4.CreateTranslation(0, 1.6f * CubeModel.Value, -CubeModel.Value * 0.5f);
            gl.UniformMatrix4(modelLoc, 1, false, (float*)&mat);

            gl.BindVertexArray(_normalVAO.Cape.VertexArrayObject);
            gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
                DrawElementsType.UnsignedShort, null);

            gl.BindTexture(TextureTarget.Texture2D, 0);
        }
    }

    private unsafe void DrawSkin(GL gl)
    {
        gl.BindTexture(TextureTarget.Texture2D, _texture);

        var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");
        var modelMat = Matrix4x4.Identity;
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.Body.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        bool enable = _enableAnimation;

        modelMat = Matrix4x4.CreateTranslation(0, CubeModel.Value, 0) *
               Matrix4x4.CreateRotationZ((enable ? _skina.Head.X : HeadRotate.X) / 360) *
               Matrix4x4.CreateRotationX((enable ? _skina.Head.Y : HeadRotate.Y) / 360) *
               Matrix4x4.CreateRotationY((enable ? _skina.Head.Z : HeadRotate.Z) / 360) *
               Matrix4x4.CreateTranslation(0, CubeModel.Value * 1.5f, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.Head.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        var value = SkinType == SkinType.NewSlim ? 1.375f : 1.5f;

        modelMat = Matrix4x4.CreateTranslation(CubeModel.Value / 2, -(value * CubeModel.Value), 0) *
                Matrix4x4.CreateRotationZ((enable ? _skina.Arm.X : ArmRotate.X) / 360) *
                Matrix4x4.CreateRotationX((enable ? _skina.Arm.Y : ArmRotate.Y) / 360) *
                Matrix4x4.CreateTranslation(value * CubeModel.Value - CubeModel.Value / 2, value * CubeModel.Value, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.LeftArm.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = Matrix4x4.CreateTranslation(-CubeModel.Value / 2, -(value * CubeModel.Value), 0) *
                Matrix4x4.CreateRotationZ((enable ? -_skina.Arm.X : -ArmRotate.X) / 360) *
                Matrix4x4.CreateRotationX((enable ? -_skina.Arm.Y : -ArmRotate.Y) / 360) *
                Matrix4x4.CreateTranslation(
                    -value * CubeModel.Value + CubeModel.Value / 2, value * CubeModel.Value, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.RightArm.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = Matrix4x4.CreateTranslation(0, -1.5f * CubeModel.Value, 0) *
               Matrix4x4.CreateRotationZ((enable ? _skina.Leg.X : LegRotate.X) / 360) *
               Matrix4x4.CreateRotationX((enable ? _skina.Leg.Y : LegRotate.Y) / 360) *
               Matrix4x4.CreateTranslation(CubeModel.Value * 0.5f, -CubeModel.Value * 1.5f, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.LeftLeg.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = Matrix4x4.CreateTranslation(0, -1.5f * CubeModel.Value, 0) *
               Matrix4x4.CreateRotationZ((enable ? -_skina.Leg.X : -LegRotate.X) / 360) *
               Matrix4x4.CreateRotationX((enable ? -_skina.Leg.Y : -LegRotate.Y) / 360) *
               Matrix4x4.CreateTranslation(-CubeModel.Value * 0.5f, -CubeModel.Value * 1.5f, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_normalVAO.RightLeg.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    private unsafe void DrawSkinTop(GL gl)
    {
        gl.BindTexture(TextureTarget.Texture2D, _texture);

        var modelLoc = gl.GetUniformLocation(_shaderProgram, "self");
        var modelMat = Matrix4x4.Identity;
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.Body.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        bool enable = _enableAnimation;

        modelMat = Matrix4x4.CreateTranslation(0, CubeModel.Value, 0) *
               Matrix4x4.CreateRotationZ((enable ? _skina.Head.X : HeadRotate.X) / 360) *
               Matrix4x4.CreateRotationX((enable ? _skina.Head.Y : HeadRotate.Y) / 360) *
               Matrix4x4.CreateRotationY((enable ? _skina.Head.Z : HeadRotate.Z) / 360) *
               Matrix4x4.CreateTranslation(0, CubeModel.Value * 1.5f, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.Head.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        var value = SkinType == SkinType.NewSlim ? 1.375f : 1.5f;

        modelMat = Matrix4x4.CreateTranslation(CubeModel.Value / 2, -(value * CubeModel.Value), 0) *
                Matrix4x4.CreateRotationZ((enable ? _skina.Arm.X : ArmRotate.X) / 360) *
                Matrix4x4.CreateRotationX((enable ? _skina.Arm.Y : ArmRotate.Y) / 360) *
                Matrix4x4.CreateTranslation(
                    value * CubeModel.Value - CubeModel.Value / 2, value * CubeModel.Value, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.LeftArm.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = Matrix4x4.CreateTranslation(-CubeModel.Value / 2, -(value * CubeModel.Value), 0) *
                Matrix4x4.CreateRotationZ((enable ? -_skina.Arm.X : -ArmRotate.X) / 360) *
                Matrix4x4.CreateRotationX((enable ? -_skina.Arm.Y : -ArmRotate.Y) / 360) *
                Matrix4x4.CreateTranslation(
                    -value * CubeModel.Value + CubeModel.Value / 2, value * CubeModel.Value, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.RightArm.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = Matrix4x4.CreateTranslation(0, -1.5f * CubeModel.Value, 0) *
               Matrix4x4.CreateRotationZ((enable ? _skina.Leg.X : LegRotate.X) / 360) *
               Matrix4x4.CreateRotationX((enable ? _skina.Leg.Y : LegRotate.Y) / 360) *
               Matrix4x4.CreateTranslation(CubeModel.Value * 0.5f, -CubeModel.Value * 1.5f, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.LeftLeg.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        modelMat = Matrix4x4.CreateTranslation(0, -1.5f * CubeModel.Value, 0) *
               Matrix4x4.CreateRotationZ((enable ? -_skina.Leg.X : -LegRotate.X) / 360) *
               Matrix4x4.CreateRotationX((enable ? -_skina.Leg.Y : -LegRotate.Y) / 360) *
               Matrix4x4.CreateTranslation(-CubeModel.Value * 0.5f, -CubeModel.Value * 1.5f, 0);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.BindVertexArray(_topVAO.RightLeg.VertexArrayObject);
        gl.DrawElements(PrimitiveType.Triangles, _steveModelDrawOrderCount,
           DrawElementsType.UnsignedShort, null);

        gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public unsafe void OpenGlRender(GL gl, int fb, double time)
    {
        if (_switchSkin)
        {
            LoadSkin(gl);
        }
        if (_switchModel)
        {
            LoadModel(gl);
        }
        if (_enableAnimation)
        {
            _skina.Tick(time);
        }

        if (!HaveSkin)
        {
            return;
        }

        if (Width != _width || Height != _height)
        {
            _width = (uint)Width;
            _height = (uint)Height;
            DeleteFrameBuffer(gl);
            InitFrameBuffer(gl);
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

        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        gl.ClearColor(0, 1, 0, 1);

        CheckError(gl);
        gl.Enable(EnableCap.CullFace);
        gl.Enable(EnableCap.DepthTest);
        gl.DepthMask(true);
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.UseProgram(_shaderProgram);
        CheckError(gl);

        var viewLoc = gl.GetUniformLocation(_shaderProgram, "view");
        var projectionLoc = gl.GetUniformLocation(_shaderProgram, "projection");
        var modelLoc = gl.GetUniformLocation(_shaderProgram, "model");

        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            (float)(Math.PI / 4), (float)_width / _height, 0.001f, 1000);

        var view = Matrix4x4.CreateLookAt(new(0, 0, 7), new(), new(0, 1, 0));

        if (_rotXY.X != 0 || _rotXY.Y != 0)
        {
            _last *= Matrix4x4.CreateRotationX(_rotXY.X / 360)
                    * Matrix4x4.CreateRotationY(_rotXY.Y / 360);
            _rotXY.X = 0;
            _rotXY.Y = 0;
        }

        var modelMat = _last
            * Matrix4x4.CreateTranslation(new(_xy.X, _xy.Y, 0))
            * Matrix4x4.CreateScale(_dis);

        gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
        gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMat);
        gl.UniformMatrix4(projectionLoc, 1, false, (float*)&projection);

        CheckError(gl);

        DrawSkin(gl);
        DrawCape(gl);

        if (EnableTop)
        {
            gl.Disable(EnableCap.DepthTest);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.DepthMask(false);

            DrawSkinTop(gl);

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

    private static void DeleteVAOItem(GL gl, VAOItem item)
    {
        gl.DeleteBuffer(item.VertexBufferObject);
        gl.DeleteBuffer(item.IndexBufferObject);
    }

    private static void DeleteVAO(GL gl, ModelVAO vao)
    {
        gl.DeleteVertexArray(vao.Head.VertexArrayObject);
        gl.DeleteVertexArray(vao.Body.VertexArrayObject);
        gl.DeleteVertexArray(vao.LeftArm.VertexArrayObject);
        gl.DeleteVertexArray(vao.RightArm.VertexArrayObject);
        gl.DeleteVertexArray(vao.LeftLeg.VertexArrayObject);
        gl.DeleteVertexArray(vao.RightLeg.VertexArrayObject);

        DeleteVAOItem(gl, vao.Head);
        DeleteVAOItem(gl, vao.Body);
        DeleteVAOItem(gl, vao.LeftArm);
        DeleteVAOItem(gl, vao.RightArm);
        DeleteVAOItem(gl, vao.LeftLeg);
        DeleteVAOItem(gl, vao.RightLeg);
    }

    public unsafe void OpenGlDeinit(GL gl)
    {
        _skina.Close();

        // Unbind everything
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.UseProgram(0);

        // Delete all resources.
        DeleteVAO(gl, _normalVAO);
        DeleteVAO(gl, _topVAO);

        gl.DeleteProgram(_shaderProgram);

        DeleteFrameBuffer(gl);
    }
}