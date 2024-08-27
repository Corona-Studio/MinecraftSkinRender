using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MinecraftSkinRender.Vulkan;

public partial class SkinRenderVulkan
{
    public unsafe ShaderModule CreateShaderModule(string filename)
    {
        var assm = Assembly.GetExecutingAssembly();
        string name = "MinecraftSkinRender.Vulkan.spv." + filename;
        using var item = assm.GetManifestResourceStream(name)!;
        if (item == null)
        {
            Console.WriteLine("failed to open file!");
        }
        using var mem = new MemoryStream();
        item!.CopyTo(mem);
        var data = mem.ToArray();

        var createInfo = new ShaderModuleCreateInfo
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (uint)data.Length
        };

        ShaderModule shaderModule;

        fixed (byte* codePtr = data)
        {
            createInfo.PCode = (uint*)codePtr;

            if (vk.CreateShaderModule(device, ref createInfo, null, out shaderModule) != Result.Success)
            {
                Console.WriteLine("failed to create shader module!");
            }
        }

        return shaderModule;
    }

    private unsafe void CreateGraphicsPipeline()
    {
        var vertShaderModule = CreateShaderModule("SkinV.spv");
        var fragShaderModule = CreateShaderModule("SkinF.spv");

        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo fragShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc[]
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        var bindingDescription = SkinVertex.GetBindingDescription();
        var attributeDescriptions = SkinVertex.GetAttributeDescriptions();

        fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
        fixed (DescriptorSetLayout* descriptorSetLayoutPtr = &descriptorSetLayout)
        {

            PipelineVertexInputStateCreateInfo vertexInputInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                PVertexBindingDescriptions = &bindingDescription,
                PVertexAttributeDescriptions = attributeDescriptionsPtr,
            };

            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false,
            };

            Viewport viewport = new()
            {
                X = 0,
                Y = 0,
                Width = swapChainExtent.Width,
                Height = swapChainExtent.Height,
                MinDepth = 0,
                MaxDepth = 1,
            };

            Rect2D scissor = new()
            {
                Offset = { X = 0, Y = 0 },
                Extent = swapChainExtent,
            };

            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = &viewport,
                ScissorCount = 1,
                PScissors = &scissor,
            };

            PipelineRasterizationStateCreateInfo rasterizer = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1,
                CullMode = CullModeFlags.BackBit,
                FrontFace = FrontFace.CounterClockwise,
                DepthBiasEnable = false,
            };

            PipelineMultisampleStateCreateInfo multisampling = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.Count1Bit,
            };

            PipelineDepthStencilStateCreateInfo depthStencil = new()
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.Less,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false,
            };

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                BlendEnable = false,
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment,
            };

            colorBlending.BlendConstants[0] = 0;
            colorBlending.BlendConstants[1] = 0;
            colorBlending.BlendConstants[2] = 0;
            colorBlending.BlendConstants[3] = 0;

            PipelineLayoutCreateInfo pipelineLayoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                PushConstantRangeCount = 0,
                SetLayoutCount = 1,
                PSetLayouts = descriptorSetLayoutPtr
            };

            if (vk.CreatePipelineLayout(device, ref pipelineLayoutInfo, null, out pipelineLayout) != Result.Success)
            {
                throw new Exception("failed to create pipeline layout!");
            }

            GraphicsPipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                Layout = pipelineLayout,
                RenderPass = renderPass,
                Subpass = 0,
                BasePipelineHandle = default
            };

            if (vk.CreateGraphicsPipelines(device, default, 1, ref pipelineInfo, null, out graphicsPipeline) != Result.Success)
            {
                throw new Exception("failed to create graphics pipeline!");
            }
        }

        vk.DestroyShaderModule(device, fragShaderModule, null);
        vk.DestroyShaderModule(device, vertShaderModule, null);

        SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        SilkMarshal.Free((nint)fragShaderStageInfo.PName);
    }

    private unsafe void CreateCommandPool()
    {
        var queueFamiliyIndicies = FindQueueFamilies(physicalDevice);

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
        };

        if (vk.CreateCommandPool(device, ref poolInfo, null, out commandPool) != Result.Success)
        {
            throw new Exception("failed to create command pool!");
        }
    }

    private CommandBuffer BeginSingleTimeCommands()
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1,
        };

        vk!.AllocateCommandBuffers(device, ref allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        vk!.BeginCommandBuffer(commandBuffer, ref beginInfo);

        return commandBuffer;
    }

    private unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        vk!.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        vk!.QueueSubmit(graphicsQueue, 1, ref submitInfo, default);
        vk!.QueueWaitIdle(graphicsQueue);

        vk!.FreeCommandBuffers(device, commandPool, 1, ref commandBuffer);
    }

    private unsafe void CreateDrawModelCommand(int i, int a, Buffer buffer, Buffer index, uint indexlen)
    {
        //Draw command
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
            Framebuffer = swapChainFramebuffers[a],
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
                Color = new (){ Float32_0 = 0, Float32_1 = 1, Float32_2 = 0, Float32_3 = 1 },
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

        vk.CmdBindPipeline(commandBuffers[i], PipelineBindPoint.Graphics, graphicsPipeline);

        var vertexBuffers = new Buffer[]
        {
                buffer,
        };
        var offsets = new ulong[] { 0 };

        fixed (ulong* offsetsPtr = offsets)
        fixed (Buffer* vertexBuffersPtr = vertexBuffers)
        {
            vk.CmdBindVertexBuffers(commandBuffers[i], 0, 1, vertexBuffersPtr, offsetsPtr);
        }

        vk.CmdBindIndexBuffer(commandBuffers[i], index, 0, IndexType.Uint16);
        vk.CmdBindDescriptorSets(commandBuffers[i], PipelineBindPoint.Graphics, pipelineLayout, 0, 1, ref descriptorSets[a], 0, null);
        vk.CmdDrawIndexed(commandBuffers[i], indexlen, 1, 0, 0, 0);
        vk.CmdEndRenderPass(commandBuffers[i]);

        if (vk.EndCommandBuffer(commandBuffers[i]) != Result.Success)
        {
            throw new Exception("failed to record command buffer!");
        }
    }

    private unsafe void CreateCommandBuffers()
    {
        commandBuffers = new CommandBuffer[13 * swapChainFramebuffers.Length];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)commandBuffers.Length,
        };

        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
        {
            if (vk.AllocateCommandBuffers(device, ref allocInfo, commandBuffersPtr) != Result.Success)
            {
                throw new Exception("failed to allocate command buffers!");
            }
        }

        for (int a = 0; a < swapChainFramebuffers.Length; a++)
        {
            CreateDrawModelCommand(a * 13 + 0, a, draw.Head.VertexBuffer, draw.Head.IndexBuffer, (uint)model.Head.Indices.Length);
            CreateDrawModelCommand(a * 13 + 1, a, draw.Body.VertexBuffer, draw.Body.IndexBuffer, (uint)model.Body.Indices.Length);
            CreateDrawModelCommand(a * 13 + 2, a, draw.LeftArm.VertexBuffer, draw.LeftArm.IndexBuffer, (uint)model.LeftArm.Indices.Length);
            CreateDrawModelCommand(a * 13 + 3, a, draw.RightArm.VertexBuffer, draw.RightArm.IndexBuffer, (uint)model.RightArm.Indices.Length);
            CreateDrawModelCommand(a * 13 + 4, a, draw.LeftLeg.VertexBuffer, draw.LeftLeg.IndexBuffer, (uint)model.LeftLeg.Indices.Length);
            CreateDrawModelCommand(a * 13 + 5, a, draw.RightLeg.VertexBuffer, draw.RightLeg.IndexBuffer, (uint)model.RightLeg.Indices.Length);
            CreateDrawModelCommand(a * 13 + 6, a, draw.TopHead.VertexBuffer, draw.TopHead.IndexBuffer, (uint)model.TopHead.Indices.Length);
            CreateDrawModelCommand(a * 13 + 7, a, draw.TopBody.VertexBuffer, draw.TopBody.IndexBuffer, (uint)model.TopBody.Indices.Length);
            CreateDrawModelCommand(a * 13 + 8, a, draw.TopLeftArm.VertexBuffer, draw.TopLeftArm.IndexBuffer, (uint)model.TopLeftArm.Indices.Length);
            CreateDrawModelCommand(a * 13 + 9, a, draw.TopRightArm.VertexBuffer, draw.TopRightArm.IndexBuffer, (uint)model.TopRightArm.Indices.Length);
            CreateDrawModelCommand(a * 13 + 10, a, draw.TopLeftLeg.VertexBuffer, draw.TopLeftLeg.IndexBuffer, (uint)model.TopLeftLeg.Indices.Length);
            CreateDrawModelCommand(a * 13 + 11, a, draw.TopRightLeg.VertexBuffer, draw.TopRightLeg.IndexBuffer, (uint)model.TopRightLeg.Indices.Length);
            CreateDrawModelCommand(a * 13 + 12, a, draw.Cape.VertexBuffer, draw.Cape.IndexBuffer, (uint)model.Cape.Indices.Length);
        }
    }
}
