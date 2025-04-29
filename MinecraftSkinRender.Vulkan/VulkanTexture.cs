﻿using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MinecraftSkinRender.Vulkan;

public partial class SkinRenderVulkan
{
    private unsafe void CreateBuffer(ulong size, BufferUsageFlags usage,
        MemoryPropertyFlags properties, ref Buffer buffer, ref DeviceMemory bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
        };

        fixed (Buffer* bufferPtr = &buffer)
        {
            if (vk!.CreateBuffer(_device, ref bufferInfo, null, bufferPtr) != Result.Success)
            {
                throw new Exception("failed to create vertex buffer!");
            }
        }

        MemoryRequirements memRequirements = new();
        vk.GetBufferMemoryRequirements(_device, buffer, out memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
        {
            if (vk!.AllocateMemory(_device, ref allocateInfo, null, bufferMemoryPtr) != Result.Success)
            {
                throw new Exception("failed to allocate vertex buffer memory!");
            }
        }

        vk.BindBufferMemory(_device, buffer, bufferMemory, 0);
    }

    private unsafe void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout, CommandPool commandPool, Queue queue)
    {
        var commandBuffer = BeginSingleTimeCommands(commandPool);

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }
        };

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;

            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;

            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else
        {
            throw new Exception("unsupported layout transition!");
        }

        vk.CmdPipelineBarrier(commandBuffer, sourceStage, destinationStage, 0, 0, null, 0, null, 1, ref barrier);

        EndSingleTimeCommands(commandBuffer, commandPool, queue);
    }

    private void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height, CommandPool commandPool, Queue queue)
    {
        var commandBuffer = BeginSingleTimeCommands(commandPool);

        BufferImageCopy region = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D(width, height, 1),

        };

        vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, ref region);

        EndSingleTimeCommands(commandBuffer, commandPool, queue);
    }

    protected unsafe void DeleteTexture()
    {
        if (textureSkinImage.Handle != 0)
        {
            vk.DestroyImage(_device, textureSkinImage, null);
        }
        if (textureSkinImageMemory.Handle != 0)
        {
            vk.FreeMemory(_device, textureSkinImageMemory, null);
        }
        if (textureSkinImageView.Handle != 0)
        {
            vk.DestroyImageView(_device, textureSkinImageView, null);
        }
        if (textureCapeImage.Handle != 0)
        {
            vk.DestroyImage(_device, textureCapeImage, null);
        }
        if (textureCapeImageMemory.Handle != 0)
        {
            vk.FreeMemory(_device, textureCapeImageMemory, null);
        }
        if (textureCapeImageView.Handle != 0)
        {
            vk.DestroyImageView(_device, textureCapeImageView, null);
        }
    }

    private unsafe void CreateCapeTexture(CommandPool commandPool, Queue queue)
    {
        HaveCape = false;

        if (_cape == null)
        {
            return;
        }
        ulong imageSize = (ulong)(_cape.Width * _cape.Height * _cape.BytesPerPixel);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit
            | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk.MapMemory(_device, stagingBufferMemory, 0, imageSize, 0, &data);
        System.Buffer.MemoryCopy((void*)_cape.GetPixels(), data, imageSize, imageSize);
        vk.UnmapMemory(_device, stagingBufferMemory);

        var fmt = Format.R8G8B8A8Srgb;
        if (_cape.ColorType == SkiaSharp.SKColorType.Bgra8888)
        {
            fmt = Format.B8G8R8A8Srgb;
        }

        CreateImage((uint)_cape.Width, (uint)_cape.Height, fmt, ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit, ref textureCapeImage, ref textureCapeImageMemory);

        TransitionImageLayout(textureCapeImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined,
            ImageLayout.TransferDstOptimal, commandPool, queue);
        CopyBufferToImage(stagingBuffer, textureCapeImage, (uint)_cape.Width, (uint)_cape.Height, commandPool, queue);
        TransitionImageLayout(textureCapeImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal,
            ImageLayout.ShaderReadOnlyOptimal, commandPool, queue);

        vk.DestroyBuffer(_device, stagingBuffer, null);
        vk.FreeMemory(_device, stagingBufferMemory, null);

        textureCapeImageView = CreateImageView(textureCapeImage, fmt, ImageAspectFlags.ColorBit);

        HaveCape = true;
    }

    private unsafe void CreateSkinTexture(CommandPool commandPool, Queue queue)
    {
        HaveSkin = false;

        if (_skinTex == null)
        {
            return;
        }
        ulong imageSize = (ulong)(_skinTex.Width * _skinTex.Height * _skinTex.BytesPerPixel);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit
            | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk.MapMemory(_device, stagingBufferMemory, 0, imageSize, 0, &data);
        System.Buffer.MemoryCopy((void*)_skinTex.GetPixels(), data, imageSize, imageSize);
        vk.UnmapMemory(_device, stagingBufferMemory);

        var fmt = Format.R8G8B8A8Srgb;
        if (_skinTex.ColorType == SkiaSharp.SKColorType.Bgra8888)
        {
            fmt = Format.B8G8R8A8Srgb;
        }

        CreateImage((uint)_skinTex.Width, (uint)_skinTex.Height, fmt, ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit, ref textureSkinImage, ref textureSkinImageMemory);

        TransitionImageLayout(textureSkinImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined,
            ImageLayout.TransferDstOptimal, commandPool, queue);
        CopyBufferToImage(stagingBuffer, textureSkinImage, (uint)_skinTex.Width, (uint)_skinTex.Height, commandPool, queue);
        TransitionImageLayout(textureSkinImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal,
            ImageLayout.ShaderReadOnlyOptimal, commandPool, queue);

        vk.DestroyBuffer(_device, stagingBuffer, null);
        vk.FreeMemory(_device, stagingBufferMemory, null);

        textureSkinImageView = CreateImageView(textureSkinImage, fmt, ImageAspectFlags.ColorBit);

        HaveSkin = true;
    }

    private void CreateTexture(CommandPool commandPool, Queue queue)
    {
        CreateSkinTexture(commandPool, queue);
        CreateCapeTexture(commandPool, queue);

        _switchSkin = false;
    }

    private unsafe void CreateTextureSampler()
    {
        vk.GetPhysicalDeviceProperties(PhysicalDevice, out var properties);

        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Nearest,
            MinFilter = Filter.Nearest,
            AddressModeU = SamplerAddressMode.ClampToBorder,
            AddressModeV = SamplerAddressMode.ClampToBorder,
            AddressModeW = SamplerAddressMode.ClampToBorder,
            AnisotropyEnable = true,
            MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
            BorderColor = BorderColor.IntTransparentBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
        };

        fixed (Sampler* textureSamplerPtr = &textureSampler)
        {
            if (vk.CreateSampler(_device, ref samplerInfo, null, textureSamplerPtr) != Result.Success)
            {
                throw new Exception("failed to create texture sampler!");
            }
        }
    }
}
