using System.Reflection;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MinecraftSkinRender.Vulkan;

public partial class SkinRenderVulkan
{
    private unsafe ShaderModule CreateShaderModule(string filename)
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

            if (vk.CreateShaderModule(_device, ref createInfo, null, out shaderModule) != Result.Success)
            {
                throw new Exception("failed to create shader module!");
            }
        }

        return shaderModule;
    }

    protected unsafe void CreateGraphicsPipeline(RenderPass renderPass)
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
                Width = _width,
                Height = _height,
                MinDepth = 0,
                MaxDepth = 1,
            };

            Rect2D scissor = new()
            {
                Offset = { X = 0, Y = 0 },
                Extent = { Width = _width, Height = _height },
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
                BlendEnable = false
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

            if (vk.CreatePipelineLayout(_device, ref pipelineLayoutInfo, null, out pipelineLayout) != Result.Success)
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

            if (vk.CreateGraphicsPipelines(_device, default, 1, ref pipelineInfo, null, out graphicsPipeline) != Result.Success)
            {
                throw new Exception("failed to create graphics pipeline!");
            }

            colorBlendAttachment.BlendEnable = true;
            colorBlendAttachment.SrcColorBlendFactor = BlendFactor.SrcAlpha;
            colorBlendAttachment.DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha;
            colorBlendAttachment.ColorBlendOp = BlendOp.Add;
            colorBlendAttachment.SrcAlphaBlendFactor = BlendFactor.One;
            colorBlendAttachment.DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha;
            colorBlendAttachment.AlphaBlendOp = BlendOp.Add;

            depthStencil.DepthWriteEnable = false;

            if (vk.CreateGraphicsPipelines(_device, default, 1, ref pipelineInfo, null, out graphicsPipelineTop) != Result.Success)
            {
                throw new Exception("failed to create graphics pipeline!");
            }
        }

        vk.DestroyShaderModule(_device, fragShaderModule, null);
        vk.DestroyShaderModule(_device, vertShaderModule, null);

        SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        SilkMarshal.Free((nint)fragShaderStageInfo.PName);
    }

    private CommandBuffer BeginSingleTimeCommands(CommandPool commandPool)
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = commandPool,
            CommandBufferCount = 1,
        };

        vk!.AllocateCommandBuffers(_device, ref allocateInfo, out CommandBuffer commandBuffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };

        vk!.BeginCommandBuffer(commandBuffer, ref beginInfo);

        return commandBuffer;
    }

    private unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer, CommandPool commandPool, Queue graphicsQueue)
    {
        vk!.EndCommandBuffer(commandBuffer);

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
        };

        vk.QueueSubmit(graphicsQueue, 1, ref submitInfo, default);
        vk.QueueWaitIdle(graphicsQueue);

        vk.FreeCommandBuffers(_device, commandPool, 1, ref commandBuffer);
    }

    public unsafe void CreateCommandBuffers(CommandBuffer buffer, int length, int index, Framebuffer framebuffer)
    {
        //Draw command
        void Push(SkinDrawPart draw, int part, bool cape = false)
        {
            Buffer[] vertexBuffers = [draw.VertexBuffer];
            ulong[] offsets = [0];

            fixed (ulong* offsetsPtr = offsets)
            fixed (Buffer* vertexBuffersPtr = vertexBuffers)
            {
                vk.CmdBindVertexBuffers(buffer, 0, 1, vertexBuffersPtr, offsetsPtr);
            }

            uint offset = (uint)(part * (int)UniformDynamicAlignment);

            vk.CmdBindIndexBuffer(buffer, draw.IndexBuffer, 0, IndexType.Uint16);
            vk.CmdBindDescriptorSets(buffer, PipelineBindPoint.Graphics,
                pipelineLayout, 0, 1, ref DescriptorSets[cape ? index + length : index], 1, ref offset);
            vk.CmdDrawIndexed(buffer, draw.IndexLen, 1, 0, 0, 0);
        }

        vk.CmdBindPipeline(buffer, PipelineBindPoint.Graphics, graphicsPipeline);

        Push(draw.Body, SkinPartIndex.Body);
        Push(draw.Head, SkinPartIndex.Head);
        Push(draw.LeftArm, SkinPartIndex.LeftArm);
        Push(draw.RightArm, SkinPartIndex.RightArm);
        Push(draw.LeftLeg, SkinPartIndex.LeftLeg);
        Push(draw.RightLeg, SkinPartIndex.RightLeg);

        if (EnableTop)
        {
            vk.CmdBindPipeline(buffer, PipelineBindPoint.Graphics, graphicsPipelineTop);

            Push(draw.TopBody, SkinPartIndex.TopBody);
            Push(draw.TopHead, SkinPartIndex.TopHead);
            Push(draw.TopLeftArm, SkinPartIndex.TopLeftArm);
            Push(draw.TopRightArm, SkinPartIndex.TopRightArm);
            Push(draw.TopLeftLeg, SkinPartIndex.TopLeftLeg);
            Push(draw.TopRightLeg, SkinPartIndex.TopRightLeg);
        }

        if (EnableCape && HaveCape)
        {
            vk.CmdBindPipeline(buffer, PipelineBindPoint.Graphics, graphicsPipeline);

            Push(draw.Cape, SkinPartIndex.Cape, true);
        }

        _switchBack = false;
        _switchType = false;
    }

    protected unsafe void DeleteGraphicsPipeline()
    {
        vk.DestroyPipeline(_device, graphicsPipeline, null);
        vk.DestroyPipeline(_device, graphicsPipelineTop, null);
        vk.DestroyPipelineLayout(_device, pipelineLayout, null);
    }
}
