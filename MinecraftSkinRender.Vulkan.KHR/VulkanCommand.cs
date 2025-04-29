using Silk.NET.Vulkan;

namespace MinecraftSkinRender.Vulkan.KHR;

public partial class SkinRenderVulkanKHR
{
    private unsafe void CreateCommandPool()
    {
        var queueFamiliyIndicies = FindQueueFamilies(PhysicalDevice);

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
        };

        if (vk.CreateCommandPool(_device, ref poolInfo, null, out commandPool) != Result.Success)
        {
            throw new Exception("failed to create command pool!");
        }
    }

    private unsafe void CreateCommandBuffers()
    {
        commandBuffers ??= new CommandBuffer[swapChainFramebuffers.Length];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)commandBuffers.Length,
        };

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            if (vk.AllocateCommandBuffers(_device, ref allocInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }

        for (int i = 0; i < swapChainFramebuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
            };

            if (vk.BeginCommandBuffer(commandBuffers[i], ref beginInfo) != Result.Success)
            {
                throw new Exception("failed to begin recording command buffer!");
            }

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = swapChainFramebuffers[i],
                RenderArea =
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = swapChainExtent,
                }
            };

            var clearValues = new ClearValue[]
            {
                new()
                {
                    Color = new (){ Float32_0 = BackColor.X, Float32_1 = BackColor.Y, Float32_2 = BackColor.Z, Float32_3 = BackColor.W },
                },
                new()
                {
                    DepthStencil = new () { Depth = 1, Stencil = 0 }
                }
            };

            fixed (ClearValue* clearValuesPtr = clearValues)
            {
                renderPassInfo.ClearValueCount = (uint)clearValues.Length;
                renderPassInfo.PClearValues = clearValuesPtr;

                vk.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
            }

            CreateCommandBuffers(commandBuffers[i], swapChainFramebuffers.Length, i, swapChainFramebuffers[i]);

            vk.CmdEndRenderPass(commandBuffers[i]);

            if (vk.EndCommandBuffer(commandBuffers[i]) != Result.Success)
            {
                throw new Exception("failed to record command buffer!");
            }
        }
    }

    protected unsafe void DeleteCommandBuffers()
    {
        fixed (CommandBuffer* commandBuffersPtr = commandBuffers!)
        {
            vk.FreeCommandBuffers(_device, commandPool, (uint)commandBuffers!.Length, commandBuffersPtr);
        }
    }
}
