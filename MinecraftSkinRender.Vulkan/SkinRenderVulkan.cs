using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace MinecraftSkinRender.Vulkan;

public partial class SkinRenderVulkan(Vk vk, IVkSurface ivk) : SkinRender
{
    private uint _width, _height;

    const int MAX_FRAMES_IN_FLIGHT = 2;

    public const bool EnableValidationLayers = true;

    private readonly string[] validationLayers =
    [
        "VK_LAYER_KHRONOS_validation"
    ];

    private readonly string[] deviceExtensions =
    [
        KhrSwapchain.ExtensionName
    ];
    private Instance instance;

    private ExtDebugUtils? debugUtils;
    private DebugUtilsMessengerEXT debugMessenger;
    private KhrSurface? khrSurface;
    private SurfaceKHR surface;

    private PhysicalDevice physicalDevice;
    private Device device;

    private Queue graphicsQueue;
    private Queue presentQueue;

    private KhrSwapchain khrSwapChain;
    private SwapchainKHR swapChain;
    private Image[] swapChainImages;
    private Format swapChainImageFormat;
    private Extent2D swapChainExtent;
    private ImageView[] swapChainImageViews;
    private Framebuffer[] swapChainFramebuffers;

    private RenderPass renderPass;
    private DescriptorSetLayout descriptorSetLayout;
    private PipelineLayout pipelineLayout;
    private Pipeline graphicsPipeline;
    private Pipeline graphicsPipelineTop;

    private CommandPool commandPool;

    private Image depthImage;
    private DeviceMemory depthImageMemory;
    private ImageView depthImageView;

    private Image textureSkinImage;
    private DeviceMemory textureSkinImageMemory;
    private ImageView textureSkinImageView;

    private Image textureCapeImage;
    private DeviceMemory textureCapeImageMemory;
    private ImageView textureCapeImageView;

    private Sampler textureSampler;

    private Semaphore[] imageAvailableSemaphores;
    private Semaphore[] renderFinishedSemaphores;
    private Fence[] inFlightFences;
    private Fence[] imagesInFlight;
    private int currentFrame = 0;

    private bool frameBufferResized = false;

    private readonly SkinModel model = new();
    private readonly SkinDraw draw = new();

    private CommandBuffer[]? commandBuffers;
    private UniformBufferObject ubo = new();

    public void VulkanInit()
    {
        _width = (uint)Width;
        _height = (uint)Height;

        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        CreateCommandPool();
        CreateDepthResources();
        CreateFramebuffers();
        CreateTexture();
        CreateTextureSampler();
        CreateModel();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    private void RecreateSwapChain()
    {
        vk.DeviceWaitIdle(device);

        CleanUpSwapChain();

        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateDepthResources();
        CreateFramebuffers();
        CreateUniformBuffers();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();

        imagesInFlight = new Fence[swapChainImages.Length];
    }

    public unsafe void VulkanRender()
    {
        if (!HaveSkin)
        {
            return;
        }

        if (Width == 0 || Height == 0)
        {
            return;
        }

        if (Width != _width || Height != _height)
        {
            _width = (uint)Width;
            _height = (uint)Height;
            frameBufferResized = true;
        }

        if (_width == 0 || _height == 0)
        {
            return;
        }

        vk.WaitForFences(device, 1, ref inFlightFences[currentFrame], true, ulong.MaxValue);

        if (_switchType || _switchSkin || _switchModel)
        {
            vk.DeviceWaitIdle(device);

            DeleteCommandBuffers();

            if (_switchSkin)
            {
                //DeleteDescriptorPool();

                DeleteTexture();
                CreateTexture();

                //CreateDescriptorPool();
                ResetDescriptorPool();
                CreateDescriptorSets();
            }

            if (_switchModel)
            {
                DeleteModel();
                CreateModel();
            }

            CreateCommandBuffers();
        }

        uint imageIndex = 0;
        var result = khrSwapChain!.AcquireNextImage(device, swapChain, ulong.MaxValue,
            imageAvailableSemaphores![currentFrame], default, ref imageIndex);

        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapChain();
            return;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("failed to acquire swap chain image!");
        }

        UpdateUniformBuffer();

        if (imagesInFlight[imageIndex].Handle != default)
        {
            vk.WaitForFences(device, 1, ref imagesInFlight[imageIndex], true, ulong.MaxValue);
        }
        imagesInFlight[imageIndex] = inFlightFences[currentFrame];

        var waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };
        var signalSemaphores = stackalloc[] { renderFinishedSemaphores![currentFrame] };

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            SignalSemaphoreCount = 1,
            CommandBufferCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        vk.ResetFences(device, 1, ref inFlightFences[currentFrame]);

        DrawSkin(submitInfo, imageIndex);

        var swapChains = stackalloc[] { swapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,

            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,

            SwapchainCount = 1,
            PSwapchains = swapChains,

            PImageIndices = &imageIndex
        };

        result = khrSwapChain.QueuePresent(presentQueue, ref presentInfo);

        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || frameBufferResized)
        {
            frameBufferResized = false;
            RecreateSwapChain();
        }
        else if (result != Result.Success)
        {
            throw new Exception("failed to present swap chain image!");
        }

        currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
    }

    public unsafe void VulkanDeinit()
    {
        CleanUpSwapChain();
        DeleteModel();
        DeleteTexture();

        vk.DestroySampler(device, textureSampler, null);

        vk.DestroyDescriptorSetLayout(device, descriptorSetLayout, null);

        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            vk.DestroySemaphore(device, renderFinishedSemaphores[i], null);
            vk.DestroySemaphore(device, imageAvailableSemaphores[i], null);
            vk.DestroyFence(device, inFlightFences![i], null);
        }

        vk.DestroyCommandPool(device, commandPool, null);

        vk.DestroyDevice(device, null);

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }

        khrSurface!.DestroySurface(instance, surface, null);
        vk.DestroyInstance(instance, null);
    }

    private unsafe void CreateSyncObjects()
    {
        imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
        imagesInFlight = new Fence[swapChainImages!.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            if (vk.CreateSemaphore(device, ref semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success ||
                vk.CreateSemaphore(device, ref semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success ||
                vk.CreateFence(device, ref fenceInfo, null, out inFlightFences[i]) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
        }
    }

    private unsafe void DrawSkin(SubmitInfo submitInfo, uint currentImage)
    {
        SetUniformBuffer(draw.Head, currentImage, GetMatrix4(MatrPartType.Head));
        SetUniformBuffer(draw.Body, currentImage, GetMatrix4(MatrPartType.Body));
        SetUniformBuffer(draw.LeftArm, currentImage, GetMatrix4(MatrPartType.LeftArm));
        SetUniformBuffer(draw.RightArm, currentImage, GetMatrix4(MatrPartType.RightArm));
        SetUniformBuffer(draw.LeftLeg, currentImage, GetMatrix4(MatrPartType.LeftLeg));
        SetUniformBuffer(draw.RightLeg, currentImage, GetMatrix4(MatrPartType.RightLeg));

        SetUniformBuffer(draw.TopHead, currentImage, GetMatrix4(MatrPartType.Head));
        SetUniformBuffer(draw.TopBody, currentImage, GetMatrix4(MatrPartType.Body));
        SetUniformBuffer(draw.TopLeftArm, currentImage, GetMatrix4(MatrPartType.LeftArm));
        SetUniformBuffer(draw.TopRightArm, currentImage, GetMatrix4(MatrPartType.RightArm));
        SetUniformBuffer(draw.TopLeftLeg, currentImage, GetMatrix4(MatrPartType.LeftLeg));
        SetUniformBuffer(draw.TopRightLeg, currentImage, GetMatrix4(MatrPartType.RightLeg));

        SetUniformBuffer(draw.Cape, currentImage, GetMatrix4(MatrPartType.Cape));

        var command1 = commandBuffers![currentImage];

        var info = submitInfo with
        {
            PCommandBuffers = &command1
        };

        if (vk.QueueSubmit(graphicsQueue, 1, ref info, inFlightFences[currentFrame]) != Result.Success)
        {
            throw new Exception("failed to submit draw command buffer!");
        }
    }

    private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            vk!.GetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);

            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
            {
                return format;
            }
        }

        throw new Exception("failed to find supported format!");
    }

    private unsafe void CreateDescriptorSetLayout()
    {
        DescriptorSetLayoutBinding uboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
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

            if (vk!.CreateDescriptorSetLayout(device, ref layoutInfo, null, descriptorSetLayoutPtr) != Result.Success)
            {
                throw new Exception("failed to create descriptor set layout!");
            }
        }
    }

    private Format FindDepthFormat()
    {
        return FindSupportedFormat([Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint], ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
    }

    private unsafe void CreateRenderPass()
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = swapChainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };

        AttachmentDescription depthAttachment = new()
        {
            Format = FindDepthFormat(),
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };

        AttachmentReference depthAttachmentRef = new()
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal,
        };

        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef,
        };

        SubpassDependency dependency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
        };

        var attachments = new[] { colorAttachment, depthAttachment };

        fixed (AttachmentDescription* attachmentsPtr = attachments)
        {
            RenderPassCreateInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = attachmentsPtr,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency,
            };

            if (vk.CreateRenderPass(device, ref renderPassInfo, null, out renderPass) != Result.Success)
            {
                throw new Exception("failed to create render pass!");
            }
        }
    }

    private unsafe void SetupDebugMessenger()
    {
        if (!EnableValidationLayers) return;

        //TryGetInstanceExtension equivilant to method CreateDebugUtilsMessengerEXT from original tutorial.
        if (!vk!.TryGetInstanceExtension(instance, out debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }

    private unsafe bool CheckValidationLayerSupport()
    {
        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }

    private unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
    }

    private unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        Console.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

        return Vk.False;
    }
}
