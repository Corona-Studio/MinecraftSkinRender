using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MinecraftSkinRender.Vulkan;

public partial class SkinRenderVulkan(Vk vk) : SkinRender
{
    protected uint _width, _height;

    public const int PartCount = 13;

    public Device Device => _device;
    protected Device _device;

    public PhysicalDevice PhysicalDevice => _physicalDevice;
    protected PhysicalDevice _physicalDevice;

    private DescriptorSetLayout descriptorSetLayout;
    private PipelineLayout pipelineLayout;
    private Pipeline graphicsPipeline;
    private Pipeline graphicsPipelineTop;

    private Image textureSkinImage;
    private DeviceMemory textureSkinImageMemory;
    private ImageView textureSkinImageView;

    protected bool commandReload;

    protected Image textureCapeImage;
    protected DeviceMemory textureCapeImageMemory;
    protected ImageView textureCapeImageView;

    protected Sampler textureSampler;

    protected Buffer[] UniformBuffers;
    protected DeviceMemory[] UniformBuffersMemory;
    protected IntPtr[] UniformBuffersPtr;
    protected ulong UniformDynamicAlignment;

    protected DescriptorPool DescriptorPool;
    protected DescriptorSet[] DescriptorSets;

    protected bool frameBufferResized = false;

    protected readonly SkinModel model = new();
    protected readonly SkinDraw draw = new();

    protected readonly UniformBufferObject[] ubo = new UniformBufferObject[PartCount];

    public void SkinInit(int length, CommandPool commandPool, Queue queue, RenderPass renderPass)
    {
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline(renderPass);
        CreateTexture(commandPool, queue);
        CreateTextureSampler();
        CreateModel(commandPool, queue);
        CreateUniformBuffers(length);
        CreateDescriptorPool((uint)length);
        CreateDescriptorSets(length);
    }

    public unsafe void SkinRender(uint imageIndex, int length, CommandPool commandPool, Queue queue)
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

        if (_switchType || _switchSkin || _switchModel)
        {
            vk.DeviceWaitIdle(_device);

            if (_switchSkin)
            {
                DeleteTexture();
                CreateTexture(commandPool, queue);

                //DeleteDescriptorPool();
                //CreateDescriptorPool();
                ResetDescriptorPool();
                CreateDescriptorSets(length);
            }

            if (_switchModel)
            {
                DeleteModel();
                CreateModel(commandPool, queue);
            }

            commandReload = true;
        }

        UpdateUboState();

        SetUniformBuffer(SkinPartIndex.Head, imageIndex, GetMatrix4(ModelPartType.Head));
        SetUniformBuffer(SkinPartIndex.Body, imageIndex, GetMatrix4(ModelPartType.Body));
        SetUniformBuffer(SkinPartIndex.LeftArm, imageIndex, GetMatrix4(ModelPartType.LeftArm));
        SetUniformBuffer(SkinPartIndex.RightArm, imageIndex, GetMatrix4(ModelPartType.RightArm));
        SetUniformBuffer(SkinPartIndex.LeftLeg, imageIndex, GetMatrix4(ModelPartType.LeftLeg));
        SetUniformBuffer(SkinPartIndex.RightLeg, imageIndex, GetMatrix4(ModelPartType.RightLeg));

        SetUniformBuffer(SkinPartIndex.TopHead, imageIndex, GetMatrix4(ModelPartType.Head));
        SetUniformBuffer(SkinPartIndex.TopBody, imageIndex, GetMatrix4(ModelPartType.Body));
        SetUniformBuffer(SkinPartIndex.TopLeftArm, imageIndex, GetMatrix4(ModelPartType.LeftArm));
        SetUniformBuffer(SkinPartIndex.TopRightArm, imageIndex, GetMatrix4(ModelPartType.RightArm));
        SetUniformBuffer(SkinPartIndex.TopLeftLeg, imageIndex, GetMatrix4(ModelPartType.LeftLeg));
        SetUniformBuffer(SkinPartIndex.TopRightLeg, imageIndex, GetMatrix4(ModelPartType.RightLeg));

        SetUniformBuffer(SkinPartIndex.Cape, imageIndex, GetMatrix4(ModelPartType.Cape));
    }

    public unsafe void SkinDeinit()
    {
        DeleteModel();
        DeleteTexture();

        vk.DestroySampler(_device, textureSampler, null);

        vk.DestroyDescriptorSetLayout(_device, descriptorSetLayout, null);

        vk.DestroyDevice(_device, null);
    }
}
