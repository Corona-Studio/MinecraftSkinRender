using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace MinecraftSkinRender.OpenGL;

public partial class SkinRenderOpenGL
{
    private void InitVAOItem(VAOItem item)
    {
        item.VertexBufferObject = gl.GenBuffer();
        item.IndexBufferObject = gl.GenBuffer();
    }

    private void InitVAO(ModelVAO vao)
    {
        vao.Head.VertexArrayObject = gl.GenVertexArray();
        vao.Body.VertexArrayObject = gl.GenVertexArray();
        vao.LeftArm.VertexArrayObject = gl.GenVertexArray();
        vao.RightArm.VertexArrayObject = gl.GenVertexArray();
        vao.LeftLeg.VertexArrayObject = gl.GenVertexArray();
        vao.RightLeg.VertexArrayObject = gl.GenVertexArray();
        vao.Cape.VertexArrayObject = gl.GenVertexArray();

        InitVAOItem(vao.Head);
        InitVAOItem(vao.Body);
        InitVAOItem(vao.LeftArm);
        InitVAOItem(vao.RightArm);
        InitVAOItem(vao.LeftLeg);
        InitVAOItem(vao.RightLeg);
        InitVAOItem(vao.Cape);
    }

    private unsafe void PutVAO(VAOItem vao, CubeModelItemObj model, float[] uv)
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

    private unsafe void LoadModel()
    {
        var normal = Steve3DModel.GetSteve(SkinType);
        var top = Steve3DModel.GetSteveTop(SkinType);
        var tex = Steve3DTexture.GetSteveTexture(SkinType);
        var textop = Steve3DTexture.GetSteveTextureTop(SkinType);

        _steveModelDrawOrderCount = (uint)normal.Head.Point.Length;

        PutVAO(_normalVAO.Head, normal.Head, tex.Head);
        PutVAO(_normalVAO.Body, normal.Body, tex.Body);
        PutVAO(_normalVAO.LeftArm, normal.LeftArm, tex.LeftArm);
        PutVAO(_normalVAO.RightArm, normal.RightArm, tex.RightArm);
        PutVAO(_normalVAO.LeftLeg, normal.LeftLeg, tex.LeftLeg);
        PutVAO(_normalVAO.RightLeg, normal.RightLeg, tex.RightLeg);
        PutVAO(_normalVAO.Cape, normal.Cape, tex.Cape);

        PutVAO(_topVAO.Head, top.Head, textop.Head);
        if (SkinType != SkinType.Old)
        {
            PutVAO(_topVAO.Head, top.Head, textop.Head);
            PutVAO(_topVAO.Body, top.Body, textop.Body);
            PutVAO(_topVAO.LeftArm, top.LeftArm, textop.LeftArm);
            PutVAO(_topVAO.RightArm, top.RightArm, textop.RightArm);
            PutVAO(_topVAO.LeftLeg, top.LeftLeg, textop.LeftLeg);
            PutVAO(_topVAO.RightLeg, top.RightLeg, textop.RightLeg);
        }
        _switchModel = false;
    }

    private void DeleteVAOItem(VAOItem item)
    {
        gl.DeleteBuffer(item.VertexBufferObject);
        gl.DeleteBuffer(item.IndexBufferObject);
    }

    private void DeleteVAO(ModelVAO vao)
    {
        gl.DeleteVertexArray(vao.Head.VertexArrayObject);
        gl.DeleteVertexArray(vao.Body.VertexArrayObject);
        gl.DeleteVertexArray(vao.LeftArm.VertexArrayObject);
        gl.DeleteVertexArray(vao.RightArm.VertexArrayObject);
        gl.DeleteVertexArray(vao.LeftLeg.VertexArrayObject);
        gl.DeleteVertexArray(vao.RightLeg.VertexArrayObject);

        DeleteVAOItem(vao.Head);
        DeleteVAOItem(vao.Body);
        DeleteVAOItem(vao.LeftArm);
        DeleteVAOItem(vao.RightArm);
        DeleteVAOItem(vao.LeftLeg);
        DeleteVAOItem(vao.RightLeg);
    }
}
