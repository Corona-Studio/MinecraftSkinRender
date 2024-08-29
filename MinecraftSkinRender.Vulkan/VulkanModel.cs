using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MinecraftSkinRender.Vulkan;

public partial class SkinRenderVulkan
{
    private static void CreateModelPart(SkinPart part, CubeModelItemObj item, float[] uv)
    {
        int size = item.Model.Length / 3;
        part.Indices = item.Point;
        part.Vertices = new SkinVertex[size];

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
    }

    private void CreateModel()
    {
        var cube = Steve3DModel.GetSteve(SkinType);
        var cubetop = Steve3DModel.GetSteveTop(SkinType);
        var tex = Steve3DTexture.GetSteveTexture(SkinType);
        var textop = Steve3DTexture.GetSteveTextureTop(SkinType);

        CreateModelPart(model.Head, cube.Head, tex.Head);
        CreateModelPart(model.Body, cube.Body, tex.Body);
        CreateModelPart(model.LeftArm, cube.LeftArm, tex.LeftArm);
        CreateModelPart(model.RightArm, cube.RightArm, tex.RightArm);
        CreateModelPart(model.LeftLeg, cube.LeftLeg, tex.LeftLeg);
        CreateModelPart(model.RightLeg, cube.RightLeg, tex.RightLeg);

        CreateModelPart(model.TopHead, cubetop.Head, textop.Head);
        CreateModelPart(model.TopBody, cubetop.Body, textop.Body);
        CreateModelPart(model.TopLeftArm, cubetop.LeftArm, textop.LeftArm);
        CreateModelPart(model.TopRightArm, cubetop.RightArm, textop.RightArm);
        CreateModelPart(model.TopLeftLeg, cubetop.LeftLeg, textop.LeftLeg);
        CreateModelPart(model.TopRightLeg, cubetop.RightLeg, textop.RightLeg);

        CreateModelPart(model.Cape, cube.Cape, tex.Cape);

        CreateVertexBuffer();
        CreateIndexBuffer();

        _switchModel = false;
    }

    private void DeleteModel()
    {
        DeleteVertexBuffer();
        DeleteIndexBuffer();
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

    private unsafe void CreateVertexBufferPart(SkinPart part, SkinDrawPart draw)
    {
        draw.IndexLen = (uint)model.Head.Indices.Length;
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

        CreateBuffer(bufferSize, BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit, ref draw.VertexBuffer, ref draw.VertexBufferMemory);

        CopyBuffer(stagingBuffer, draw.VertexBuffer, bufferSize);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
    }

    private unsafe void CreateVertexBuffer()
    {
        CreateVertexBufferPart(model.Head, draw.Head);
        CreateVertexBufferPart(model.Body, draw.Body);
        CreateVertexBufferPart(model.LeftArm, draw.LeftArm);
        CreateVertexBufferPart(model.RightArm, draw.RightArm);
        CreateVertexBufferPart(model.LeftLeg, draw.LeftLeg);
        CreateVertexBufferPart(model.RightLeg, draw.RightLeg);
        CreateVertexBufferPart(model.TopHead, draw.TopHead);
        CreateVertexBufferPart(model.TopBody, draw.TopBody);
        CreateVertexBufferPart(model.TopLeftArm, draw.TopLeftArm);
        CreateVertexBufferPart(model.TopRightArm, draw.TopRightArm);
        CreateVertexBufferPart(model.TopLeftLeg, draw.TopLeftLeg);
        CreateVertexBufferPart(model.TopRightLeg, draw.TopRightLeg);
        CreateVertexBufferPart(model.Cape, draw.Cape);
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


    private unsafe void DeleteUniformBuffers()
    {
        for (int i = 0; i < swapChainImages!.Length; i++)
        {
            vk.DestroyBuffer(device, UniformBuffers![i], null);
            vk.FreeMemory(device, UniformBuffersMemory![i], null);
        }
    }

    private unsafe void CreateUniformBuffers()
    {
        for (int a = 0; a < PartCount; a++)
        {
            ubo[a] = new();
        }

        ulong size = (ulong)Unsafe.SizeOf<UniformBufferObject>();

        UniformBuffers = new Buffer[swapChainImages.Length];
        UniformBuffersMemory = new DeviceMemory[swapChainImages.Length];
        UniformBuffersPtr = new IntPtr[swapChainImages.Length];

        vk.GetPhysicalDeviceProperties(physicalDevice, out var properties);
        ulong minUboAlignment = properties.Limits.MinUniformBufferOffsetAlignment;

        UniformDynamicAlignment = size;
        if (minUboAlignment > 0)
        {
            UniformDynamicAlignment = (UniformDynamicAlignment + minUboAlignment - 1) & ~(minUboAlignment - 1);
        }

        ulong bufferSize = UniformDynamicAlignment * PartCount;
        for (int i = 0; i < swapChainImages.Length; i++)
        {
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = bufferSize,
                Usage = BufferUsageFlags.UniformBufferBit,
                SharingMode = SharingMode.Exclusive,
            };

            fixed (Buffer* bufferPtr = &UniformBuffers[i])
            {
                if (vk.CreateBuffer(device, ref bufferInfo, null, bufferPtr) != Result.Success)
                {
                    throw new Exception("failed to create vertex buffer!");
                }
            }

            MemoryRequirements memRequirements = new();
            vk.GetBufferMemoryRequirements(device, UniformBuffers[i], out memRequirements);

            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
                | MemoryPropertyFlags.DeviceLocalBit),
            };

            fixed (DeviceMemory* bufferMemoryPtr = &UniformBuffersMemory[i])
            {
                if (vk.AllocateMemory(device, ref allocateInfo, null, bufferMemoryPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate vertex buffer memory!");
                }
            }

            vk.BindBufferMemory(device, UniformBuffers[i], UniformBuffersMemory[i], 0);

            void* data;
            vk.MapMemory(device, UniformBuffersMemory[i], 0, bufferSize, 0, &data);
            UniformBuffersPtr[i] = (IntPtr)data;
        }
    }

    private unsafe void SetUniformBuffer(int part, uint i, Matrix4x4 self)
    {
        ulong offset = (ulong)part * UniformDynamicAlignment;
        ubo[part].self = self;

        void* ptr = (void*)(UniformBuffersPtr[i] + (long)offset);
        UniformBufferObject obj = ubo[part];
        void* ptr1 = &obj;

        System.Buffer.MemoryCopy(ptr1, ptr, UniformDynamicAlignment, UniformDynamicAlignment);
    }

    private unsafe void UpdateUniformBuffer()
    {
        var model = GetMatrix4(MatrPartType.Model);
        var view = GetMatrix4(MatrPartType.View);
        var proj = GetMatrix4(MatrPartType.Proj);

        for (int a = 0; a < PartCount; a++)
        {
            ubo[a].model = model;
            ubo[a].view = view;
            ubo[a].proj = proj;
            ubo[a].lightColor = new(1.0f, 1.0f, 1.0f);
            ubo[a].proj.M22 *= -1;
        }
    }
}
