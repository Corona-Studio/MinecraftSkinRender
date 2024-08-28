using Silk.NET.Vulkan;
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
            if (vk!.CreateBuffer(device, ref bufferInfo, null, bufferPtr) != Result.Success)
            {
                throw new Exception("failed to create vertex buffer!");
            }
        }

        MemoryRequirements memRequirements = new();
        vk!.GetBufferMemoryRequirements(device, buffer, out memRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
        };

        fixed (DeviceMemory* bufferMemoryPtr = &bufferMemory)
        {
            if (vk!.AllocateMemory(device, ref allocateInfo, null, bufferMemoryPtr) != Result.Success)
            {
                throw new Exception("failed to allocate vertex buffer memory!");
            }
        }

        vk.BindBufferMemory(device, buffer, bufferMemory, 0);
    }

    private unsafe void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

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

        EndSingleTimeCommands(commandBuffer);
    }

    private void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        CommandBuffer commandBuffer = BeginSingleTimeCommands();

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

        EndSingleTimeCommands(commandBuffer);
    }

    private unsafe void DeleteTexture()
    {
        if (textureSkinImage.Handle != 0)
        {
            vk.DestroyImage(device, textureSkinImage, null);
        }
        if (textureSkinImageMemory.Handle != 0)
        {
            vk.FreeMemory(device, textureSkinImageMemory, null);
        }
        if (textureSkinImageView.Handle != 0)
        {
            vk.DestroyImageView(device, textureSkinImageView, null);
        }
        if (textureCapeImage.Handle != 0)
        {
            vk.DestroyImage(device, textureCapeImage, null);
        }
        if (textureCapeImageMemory.Handle != 0)
        {
            vk.FreeMemory(device, textureCapeImageMemory, null);
        }
        if (textureCapeImageView.Handle != 0) 
        {
            vk.DestroyImageView(device, textureCapeImageView, null);
        }
    }

    private unsafe void CreateCapeTexture()
    {
        HaveCape = false;

        if (Cape == null)
        {
            return;
        }
        ulong imageSize = (ulong)(Cape.Width * Cape.Height * Cape.BytesPerPixel);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit
            | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
        System.Buffer.MemoryCopy((void*)Cape.GetPixels(), data, imageSize, imageSize);
        vk.UnmapMemory(device, stagingBufferMemory);

        var fmt = Format.R8G8B8A8Srgb;
        if (Cape.ColorType == SkiaSharp.SKColorType.Bgra8888)
        {
            fmt = Format.B8G8R8A8Srgb;
        }

        CreateImage((uint)Cape.Width, (uint)Cape.Height, fmt, ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit, ref textureCapeImage, ref textureCapeImageMemory);

        TransitionImageLayout(textureCapeImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined,
            ImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, textureCapeImage, (uint)Cape.Width, (uint)Cape.Height);
        TransitionImageLayout(textureCapeImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal,
            ImageLayout.ShaderReadOnlyOptimal);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);

        textureCapeImageView = CreateImageView(textureCapeImage, fmt, ImageAspectFlags.ColorBit);

        HaveCape = true;
    }

    private unsafe void CreateSkinTexture()
    {
        HaveSkin = false;

        if (Skin == null)
        {
            return;
        }
        ulong imageSize = (ulong)(Skin.Width * Skin.Height * Skin.BytesPerPixel);

        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit
            | MemoryPropertyFlags.HostCoherentBit, ref stagingBuffer, ref stagingBufferMemory);

        void* data;
        vk.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
        System.Buffer.MemoryCopy((void*)Skin.GetPixels(), data, imageSize, imageSize);
        vk.UnmapMemory(device, stagingBufferMemory);

        var fmt = Format.R8G8B8A8Srgb;
        if (Skin.ColorType == SkiaSharp.SKColorType.Bgra8888)
        {
            fmt = Format.B8G8R8A8Srgb;
        }

        CreateImage((uint)Skin.Width, (uint)Skin.Height, fmt, ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit, ref textureSkinImage, ref textureSkinImageMemory);

        TransitionImageLayout(textureSkinImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined,
            ImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, textureSkinImage, (uint)Skin.Width, (uint)Skin.Height);
        TransitionImageLayout(textureSkinImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal,
            ImageLayout.ShaderReadOnlyOptimal);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);

        textureSkinImageView = CreateImageView(textureSkinImage, fmt, ImageAspectFlags.ColorBit);

        HaveSkin = true;
    }

    private void CreateTexture()
    {
        CreateSkinTexture();
        CreateCapeTexture();

        _switchSkin = false;
    }

    private unsafe void CreateTextureSampler()
    {
        vk.GetPhysicalDeviceProperties(physicalDevice, out PhysicalDeviceProperties properties);

        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Nearest,
            MinFilter = Filter.Nearest,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = true,
            MaxAnisotropy = properties.Limits.MaxSamplerAnisotropy,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
        };

        fixed (Sampler* textureSamplerPtr = &textureSampler)
        {
            if (vk.CreateSampler(device, ref samplerInfo, null, textureSamplerPtr) != Result.Success)
            {
                throw new Exception("failed to create texture sampler!");
            }
        }
    }
}
