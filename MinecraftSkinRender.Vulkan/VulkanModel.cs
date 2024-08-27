using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MinecraftSkinRender.Vulkan;

public partial class SkinRenderVulkan
{

    private static SkinPart CreateModelPart(CubeModelItemObj item, float[] uv)
    {
        int size = item.Model.Length / 3;
        var part = new SkinPart
        {
            Indices = item.Point,
            Vertices = new SkinVertex[size]
        };

        var vertices = CubeModel.Vertices;
        int index = 0, index1 = 0, index2 = 0;
        for (int a = 0; a < size; a++)
        {
            part.Vertices[a] = new()
            {
                Pos = new Vector3D<float>(item.Model[index++], item.Model[index++], item.Model[index++]),
                Normal = new Vector3D<float>(vertices[index1++], vertices[index1++], vertices[index1++]),
                TextCoord = new(uv[index2++], uv[index2++])
            };
        }

        return part;
    }

    private void CreateModel()
    {
        var cube = Steve3DModel.GetSteve(SkinType);
        var cubetop = Steve3DModel.GetSteveTop(SkinType);
        var tex = Steve3DTexture.GetSteveTexture(SkinType);
        var textop = Steve3DTexture.GetSteveTextureTop(SkinType);

        model = new()
        {
            Head = CreateModelPart(cube.Head, tex.Head),
            Body = CreateModelPart(cube.Body, tex.Body),
            LeftArm = CreateModelPart(cube.LeftArm, tex.LeftArm),
            RightArm = CreateModelPart(cube.RightArm, tex.RightArm),
            LeftLeg = CreateModelPart(cube.LeftLeg, tex.LeftLeg),
            RightLeg = CreateModelPart(cube.RightLeg, tex.RightLeg),
         
            TopHead = CreateModelPart(cubetop.Head, textop.Head),
            TopBody = CreateModelPart(cubetop.Body, textop.Body),
            TopLeftArm = CreateModelPart(cubetop.LeftArm, textop.LeftArm),
            TopRightArm = CreateModelPart(cubetop.RightArm, textop.RightArm),
            TopLeftLeg = CreateModelPart(cubetop.LeftLeg, textop.LeftLeg),
            TopRightLeg = CreateModelPart(cubetop.RightLeg, textop.RightLeg),
            Cape = CreateModelPart(cube.Cape, tex.Cape),
        };
    }

    private void DeleteModel()
    { 
        
    }

    private void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

        BufferCopy copyRegion = new()
        {
            Size = size,
        };

        vk!.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, ref copyRegion);

        EndSingleTimeCommands(commandBuffer);
    }

    private unsafe void DeleteVertexBufferPart(SkinDrawPart part)
    {
        if (part.VertexBuffer.Handle != 0)
        {
            vk.DestroyBuffer(device, part.VertexBuffer, null);
            part.VertexBuffer.Handle = 0;
        }
        if (part.VertexBufferMemory.Handle != 0)
        {
            vk.FreeMemory(device, part.VertexBufferMemory, null);
            part.VertexBufferMemory.Handle = 0;
        }
    }

    private unsafe void DeleteVertexBuffer()
    {
        DeleteVertexBufferPart(draw.Head);
        DeleteVertexBufferPart(draw.Body);
        DeleteVertexBufferPart(draw.LeftArm);
        DeleteVertexBufferPart(draw.RightArm);
        DeleteVertexBufferPart(draw.LeftLeg);
        DeleteVertexBufferPart(draw.RightLeg);
        DeleteVertexBufferPart(draw.TopHead);
        DeleteVertexBufferPart(draw.TopBody);
        DeleteVertexBufferPart(draw.TopLeftArm);
        DeleteVertexBufferPart(draw.TopRightArm);
        DeleteVertexBufferPart(draw.TopLeftLeg);
        DeleteVertexBufferPart(draw.TopRightLeg);
        DeleteVertexBufferPart(draw.Cape);
    }

    private unsafe SkinDrawPart CreateVertexBufferPart(SkinPart part)
    {
        var draw = new SkinDrawPart();
        ulong bufferSize = (ulong)(Unsafe.SizeOf<SkinVertex>() * model.Head.Vertices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        part.Vertices.AsSpan().CopyTo(new Span<SkinVertex>(data, part.Vertices.Length));
        vk.UnmapMemory(device, stagingBufferMemory);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref draw.VertexBuffer, ref draw.VertexBufferMemory);

        CopyBuffer(stagingBuffer, draw.VertexBuffer, bufferSize);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);

        return draw;
    }

    private unsafe void CreateVertexBuffer()
    {
        draw = new SkinDraw()
        {
            Head = CreateVertexBufferPart(model.Head),
            Body = CreateVertexBufferPart(model.Body),
            LeftArm = CreateVertexBufferPart(model.LeftArm),
            RightArm = CreateVertexBufferPart(model.RightArm),
            LeftLeg = CreateVertexBufferPart(model.LeftLeg),
            RightLeg = CreateVertexBufferPart(model.RightLeg),
            TopHead = CreateVertexBufferPart(model.TopHead),
            TopBody = CreateVertexBufferPart(model.TopBody),
            TopLeftArm = CreateVertexBufferPart(model.TopLeftArm),
            TopRightArm = CreateVertexBufferPart(model.TopRightArm),
            TopLeftLeg = CreateVertexBufferPart(model.TopLeftLeg),
            TopRightLeg = CreateVertexBufferPart(model.TopRightLeg),
            Cape = CreateVertexBufferPart(model.Cape),
        };
    }

    private unsafe void CreateIndexBufferPart(SkinPart part, SkinDrawPart draw)
    {
        ulong bufferSize = (ulong)(Unsafe.SizeOf<ushort>() * part.Indices.Length);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk!.MapMemory(device, stagingBufferMemory, 0, bufferSize, 0, &data);
        part.Indices.AsSpan().CopyTo(new Span<ushort>(data, part.Indices.Length));
        vk!.UnmapMemory(device, stagingBufferMemory);

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.DeviceLocalBit, ref draw.IndexBuffer, ref draw.IndexBufferMemory);

        CopyBuffer(stagingBuffer, draw.IndexBuffer, bufferSize);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
    }

    private void CreateIndexBuffer()
    {
        CreateIndexBufferPart(model.Head, draw.Head);
        CreateIndexBufferPart(model.Body, draw.Body);
        CreateIndexBufferPart(model.LeftArm, draw.LeftArm);
        CreateIndexBufferPart(model.RightArm, draw.RightArm);
        CreateIndexBufferPart(model.LeftLeg, draw.LeftLeg);
        CreateIndexBufferPart(model.RightLeg, draw.RightLeg);
        CreateIndexBufferPart(model.TopHead, draw.TopHead);
        CreateIndexBufferPart(model.TopBody, draw.TopBody);
        CreateIndexBufferPart(model.TopLeftArm, draw.TopLeftArm);
        CreateIndexBufferPart(model.TopRightArm, draw.TopRightArm);
        CreateIndexBufferPart(model.TopLeftLeg, draw.TopLeftLeg);
        CreateIndexBufferPart(model.TopRightLeg, draw.TopRightLeg);
        CreateIndexBufferPart(model.Cape, draw.Cape);
    }

    private unsafe void DeleteIndexBufferPart(SkinDrawPart part)
    {
        if (part.IndexBuffer.Handle != 0)
        {
            vk.DestroyBuffer(device, part.IndexBuffer, null);
            part.IndexBuffer.Handle = 0;
        }
        if (part.IndexBufferMemory.Handle != 0)
        {
            vk.FreeMemory(device, part.IndexBufferMemory, null);
            part.IndexBufferMemory.Handle = 0;
        }
    }

    private unsafe void DeleteIndexBuffer()
    {
        DeleteIndexBufferPart(draw.Head);
        DeleteIndexBufferPart(draw.Body);
        DeleteIndexBufferPart(draw.LeftArm);
        DeleteIndexBufferPart(draw.RightArm);
        DeleteIndexBufferPart(draw.LeftLeg);
        DeleteIndexBufferPart(draw.RightLeg);
        DeleteIndexBufferPart(draw.TopHead);
        DeleteIndexBufferPart(draw.TopBody);
        DeleteIndexBufferPart(draw.TopLeftArm);
        DeleteIndexBufferPart(draw.TopRightArm);
        DeleteIndexBufferPart(draw.TopLeftLeg);
        DeleteIndexBufferPart(draw.TopRightLeg);
        DeleteIndexBufferPart(draw.Cape);
    }
}
