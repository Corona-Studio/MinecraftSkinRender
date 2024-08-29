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
    private unsafe void DeleteDescriptorPool()
    {
        if (DescriptorPool.Handle != 0)
        {
            vk.DestroyDescriptorPool(device, DescriptorPool, null);
        }
    }

    private unsafe void CreateDescriptorPool()
    {
        var poolSizes = new DescriptorPoolSize[]
        {
            new()
            {
                Type = DescriptorType.UniformBufferDynamic,
                DescriptorCount = (uint)swapChainImages.Length,
            },
            new()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = (uint)swapChainImages.Length,
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
                MaxSets = (uint)swapChainImages.Length * 2,
            };

            if (vk.CreateDescriptorPool(device, ref poolInfo, null, descriptorPoolPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor pool!");
            }
        }
    }

    private void ResetDescriptorPool()
    {
        if (DescriptorPool.Handle != 0)
        {
            vk.ResetDescriptorPool(device, DescriptorPool, 0);
        }
    }

    private unsafe void CreateDescriptorSetsPart(bool cape)
    {
        for (int i = 0; i < swapChainImages.Length; i++)
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
                    DstSet = DescriptorSets[cape ? i + swapChainImages.Length : i],
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBufferDynamic,
                    DescriptorCount = 1,
                    PBufferInfo = &vertInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = DescriptorSets[cape ? i + swapChainImages.Length : i],
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
        var layouts = new DescriptorSetLayout[swapChainImages.Length * 2];
        Array.Fill(layouts, descriptorSetLayout);

        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            DescriptorSetAllocateInfo allocateInfo = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = DescriptorPool,
                DescriptorSetCount = (uint)swapChainImages!.Length * 2,
                PSetLayouts = layoutsPtr,
            };

            DescriptorSets = new DescriptorSet[swapChainImages.Length * 2];
            fixed (DescriptorSet* descriptorSetsPtr = DescriptorSets)
            {
                if (vk.AllocateDescriptorSets(device, ref allocateInfo, descriptorSetsPtr) != Result.Success)
                {
                    throw new Exception("failed to allocate descriptor sets!");
                }
            }
        }

        CreateDescriptorSetsPart(false);
        CreateDescriptorSetsPart(true);
    }
}
