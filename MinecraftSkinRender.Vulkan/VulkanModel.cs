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

    private unsafe void CreateUniformBuffersPart(SkinDrawPart part)
    {
        ulong bufferSize = (ulong)Unsafe.SizeOf<UniformBufferObject>();

        part.uniformBuffers = new Buffer[swapChainImages.Length];
        part.uniformBuffersMemory = new DeviceMemory[swapChainImages.Length];
        part.uniformBuffersPtr = new IntPtr[swapChainImages.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = bufferSize,
                Usage = BufferUsageFlags.UniformBufferBit,
                SharingMode = SharingMode.Exclusive,
            };

            fixed (Buffer* bufferPtr = &part.uniformBuffers[i])
            {
                if (vk.CreateBuffer(device, ref bufferInfo, null, bufferPtr) != Result.Success)
                {
                    throw new Exception("failed to create vertex buffer!");
                }
            }

            MemoryRequirements memRequirements = new();
            vk.GetBufferMemoryRequirements(device, part.uniformBuffers[i], out memRequirements);

            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit
                | MemoryPropertyFlags.DeviceLocalBit),
            };

            fixed (DeviceMemory* bufferMemoryPtr = &part.uniformBuffersMemory[i])
            {
                if (vk.AllocateMemory(device, ref allocateInfo, null, bufferMemoryPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate vertex buffer memory!");
                }
            }

            vk.BindBufferMemory(device, part.uniformBuffers[i], part.uniformBuffersMemory[i], 0);

            void* data;
            vk.MapMemory(device, part.uniformBuffersMemory[i], 0, (ulong)Unsafe.SizeOf<UniformBufferObject>(), 0, &data);
            part.uniformBuffersPtr[i] = (IntPtr)data;
        }
    }

    private unsafe void DeleteUniformBufferPart(SkinDrawPart part)
    {
        for (int i = 0; i < swapChainImages!.Length; i++)
        {
            vk.DestroyBuffer(device, part.uniformBuffers![i], null);
            vk.FreeMemory(device, part.uniformBuffersMemory![i], null);
        }
    }

    private unsafe void DeleteUniformBuffers()
    {
        DeleteUniformBufferPart(draw.Head);
        DeleteUniformBufferPart(draw.Body);
        DeleteUniformBufferPart(draw.LeftArm);
        DeleteUniformBufferPart(draw.RightArm);
        DeleteUniformBufferPart(draw.LeftLeg);
        DeleteUniformBufferPart(draw.RightLeg);
        DeleteUniformBufferPart(draw.TopHead);
        DeleteUniformBufferPart(draw.TopBody);
        DeleteUniformBufferPart(draw.TopLeftArm);
        DeleteUniformBufferPart(draw.TopRightArm);
        DeleteUniformBufferPart(draw.TopLeftLeg);
        DeleteUniformBufferPart(draw.TopRightLeg);
        DeleteUniformBufferPart(draw.Cape);
    }

    private unsafe void CreateUniformBuffers()
    {
        CreateUniformBuffersPart(draw.Head);
        CreateUniformBuffersPart(draw.Body);
        CreateUniformBuffersPart(draw.LeftArm);
        CreateUniformBuffersPart(draw.RightArm);
        CreateUniformBuffersPart(draw.LeftLeg);
        CreateUniformBuffersPart(draw.RightLeg);
        CreateUniformBuffersPart(draw.TopHead);
        CreateUniformBuffersPart(draw.TopBody);
        CreateUniformBuffersPart(draw.TopLeftArm);
        CreateUniformBuffersPart(draw.TopRightArm);
        CreateUniformBuffersPart(draw.TopLeftLeg);
        CreateUniformBuffersPart(draw.TopRightLeg);
        CreateUniformBuffersPart(draw.Cape);
    }

    private unsafe void SetUniformBuffer(SkinDrawPart part, uint i, Matrix4x4 self)
    {
        ubo.self = self;
        new Span<UniformBufferObject>((void*)part.uniformBuffersPtr[i], 1)[0] = ubo;
    }

    private unsafe void UpdateUniformBuffer()
    {
        ubo.model = GetMatrix4(MatrPartType.Model);
        ubo.view = GetMatrix4(MatrPartType.View);
        ubo.proj = GetMatrix4(MatrPartType.Proj);
        ubo.lightColor = new(1.0f, 1.0f, 1.0f);
        ubo.proj.M22 *= -1;
    }

    private unsafe void DeleteDescriptorPoolPart(SkinDrawPart part)
    {
        if (part.descriptorPool.Handle != 0)
        {
            vk.DestroyDescriptorPool(device, part.descriptorPool, null);
        }
    }

    private void DeleteDescriptorPool()
    {
        DeleteDescriptorPoolPart(draw.Head);
        DeleteDescriptorPoolPart(draw.Body);
        DeleteDescriptorPoolPart(draw.LeftArm);
        DeleteDescriptorPoolPart(draw.RightArm);
        DeleteDescriptorPoolPart(draw.LeftLeg);
        DeleteDescriptorPoolPart(draw.RightLeg);
        DeleteDescriptorPoolPart(draw.TopHead);
        DeleteDescriptorPoolPart(draw.TopBody);
        DeleteDescriptorPoolPart(draw.TopLeftArm);
        DeleteDescriptorPoolPart(draw.TopRightArm);
        DeleteDescriptorPoolPart(draw.TopLeftLeg);
        DeleteDescriptorPoolPart(draw.TopRightLeg);
        DeleteDescriptorPoolPart(draw.Cape);
    }

    private unsafe void CreateDescriptorPoolPart(SkinDrawPart part)
    {
        var poolSizes = new DescriptorPoolSize[]
        {
            new()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = (uint)swapChainImages!.Length,
            },
            new()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)swapChainImages!.Length,
            },
        };

        fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
        fixed (DescriptorPool* descriptorPoolPtr = &part.descriptorPool)
        {
            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = poolSizesPtr,
                MaxSets = (uint)swapChainImages!.Length,
            };

            if (vk.CreateDescriptorPool(device, ref poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }
        }
    }

    private void CreateDescriptorPool()
    {
        CreateDescriptorPoolPart(draw.Head);
        CreateDescriptorPoolPart(draw.Body);
        CreateDescriptorPoolPart(draw.LeftArm);
        CreateDescriptorPoolPart(draw.RightArm);
        CreateDescriptorPoolPart(draw.LeftLeg);
        CreateDescriptorPoolPart(draw.RightLeg);
        CreateDescriptorPoolPart(draw.TopHead);
        CreateDescriptorPoolPart(draw.TopBody);
        CreateDescriptorPoolPart(draw.TopLeftArm);
        CreateDescriptorPoolPart(draw.TopRightArm);
        CreateDescriptorPoolPart(draw.TopLeftLeg);
        CreateDescriptorPoolPart(draw.TopRightLeg);
        CreateDescriptorPoolPart(draw.Cape);
    }

    private unsafe void ResetDescriptorPoolPart(SkinDrawPart part)
    {
        if (part.descriptorPool.Handle != 0)
        {
            vk.ResetDescriptorPool(device, part.descriptorPool, 0);
        }
    }

    private void ResetDescriptorPool()
    {
        ResetDescriptorPoolPart(draw.Head);
        ResetDescriptorPoolPart(draw.Body);
        ResetDescriptorPoolPart(draw.LeftArm);
        ResetDescriptorPoolPart(draw.RightArm);
        ResetDescriptorPoolPart(draw.LeftLeg);
        ResetDescriptorPoolPart(draw.RightLeg);
        ResetDescriptorPoolPart(draw.TopHead);
        ResetDescriptorPoolPart(draw.TopBody);
        ResetDescriptorPoolPart(draw.TopLeftArm);
        ResetDescriptorPoolPart(draw.TopRightArm);
        ResetDescriptorPoolPart(draw.TopLeftLeg);
        ResetDescriptorPoolPart(draw.TopRightLeg);
        ResetDescriptorPoolPart(draw.Cape);
    }

    private unsafe void CreateDescriptorSetsPart(SkinDrawPart part, bool cape)
    {
        var layouts = new DescriptorSetLayout[swapChainImages.Length];
        Array.Fill(layouts, descriptorSetLayout);

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = part.descriptorPool,
                DescriptorSetCount = (uint)swapChainImages!.Length,
                PSetLayouts = layoutsPtr,
            };

            part.descriptorSets = new DescriptorSet[swapChainImages.Length];
            fixed (DescriptorSet* descriptorSetsPtr = part.descriptorSets)
            {
                if (vk.AllocateDescriptorSets(device, ref allocateInfo, descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            DescriptorBufferInfo vertInfo = new()
            {
                Buffer = part.uniformBuffers[i],
                Offset = 0,
                Range = (ulong)Unsafe.SizeOf<UniformBufferObject>(),
            };

            DescriptorImageInfo imageInfo = new()
            {
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = cape ? textureCapeImageView : textureSkinImageView,
                Sampler = textureSampler,
            };

            var descriptorWrites = new WriteDescriptorSet[]
            {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = part.descriptorSets[i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &vertInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = part.descriptorSets[i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
            };

            fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
            {
                vk.UpdateDescriptorSets(device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
            }
        }
    }

    private unsafe void CreateDescriptorSets()
    {
        CreateDescriptorSetsPart(draw.Head, false);
        CreateDescriptorSetsPart(draw.Body, false);
        CreateDescriptorSetsPart(draw.LeftArm, false);
        CreateDescriptorSetsPart(draw.RightArm, false);
        CreateDescriptorSetsPart(draw.LeftLeg, false);
        CreateDescriptorSetsPart(draw.RightLeg, false);
        CreateDescriptorSetsPart(draw.TopHead, false);
        CreateDescriptorSetsPart(draw.TopBody, false);
        CreateDescriptorSetsPart(draw.TopLeftArm, false);
        CreateDescriptorSetsPart(draw.TopRightArm, false);
        CreateDescriptorSetsPart(draw.TopLeftLeg, false);
        CreateDescriptorSetsPart(draw.TopRightLeg, false);
        CreateDescriptorSetsPart(draw.Cape, true);
    }
}
