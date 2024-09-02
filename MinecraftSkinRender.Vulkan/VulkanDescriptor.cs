using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;

namespace MinecraftSkinRender.Vulkan;

public partial class SkinRenderVulkan
{
    protected unsafe void DeleteDescriptorPool()
    {
        if (DescriptorPool.Handle != 0)
        {
            vk.DestroyDescriptorPool(_device, DescriptorPool, null);
        }
    }

    protected unsafe void CreateDescriptorPool(uint length)
    {
        var poolSizes = new DescriptorPoolSize[]
        {
            new()
            {
                Type = DescriptorType.UniformBufferDynamic,
                DescriptorCount = length,
            },
            new()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = length,
            },
        };

        fixed (DescriptorPoolSize* poolSizesPtr = poolSizes)
        fixed (DescriptorPool* descriptorPoolPtr = &DescriptorPool)
        {
            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = poolSizesPtr,
                MaxSets = length * 2,
            };

            if (vk.CreateDescriptorPool(_device, ref poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }
        }
    }

    private void ResetDescriptorPool()
    {
        if (DescriptorPool.Handle != 0)
        {
            vk.ResetDescriptorPool(_device, DescriptorPool, 0);
        }
    }

    private unsafe void CreateDescriptorSetsPart(int length, bool cape)
    {
        for (int i = 0; i < length; i++)
        {
            DescriptorBufferInfo vertInfo = new()
            {
                Buffer = UniformBuffers[i],
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
                    DstSet = DescriptorSets[cape ? i + length : i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBufferDynamic,
                    DescriptorCount = 1,
                    PBufferInfo = &vertInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = DescriptorSets[cape ? i + length : i],
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
            };

            fixed (WriteDescriptorSet* descriptorWritesPtr = descriptorWrites)
            {
                vk.UpdateDescriptorSets(_device, (uint)descriptorWrites.Length, descriptorWritesPtr, 0, null);
            }
        }
    }

    protected unsafe void CreateDescriptorSets(int length)
    {
        var layouts = new DescriptorSetLayout[length * 2];
        Array.Fill(layouts, descriptorSetLayout);

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = DescriptorPool,
                DescriptorSetCount = (uint)length * 2,
                PSetLayouts = layoutsPtr,
            };

            DescriptorSets = new DescriptorSet[length * 2];
            fixed (DescriptorSet* descriptorSetsPtr = DescriptorSets)
            {
                if (vk.AllocateDescriptorSets(_device, ref allocateInfo, descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }

        CreateDescriptorSetsPart(length, false);
        CreateDescriptorSetsPart(length, true);
    }

    private unsafe void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBinding uboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            PImmutableSamplers = null,
            StageFlags = ShaderStageFlags.VertexBit,
        };

        DescriptorSetLayoutBinding samplerLayoutBinding = new()
        {
            Binding = 1,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            PImmutableSamplers = null,
            StageFlags = ShaderStageFlags.FragmentBit,
        };

        var bindings = new DescriptorSetLayoutBinding[] { uboLayoutBinding, samplerLayoutBinding };

        fixed (DescriptorSetLayoutBinding* bindingsPtr = bindings)
        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {
            DescriptorSetLayoutCreateInfo layoutInfo = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = bindingsPtr,
            };

            if (vk.CreateDescriptorSetLayout(_device, ref layoutInfo, null, descriptorSetLayoutPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor set layout!");
            }
        }
    }
}
