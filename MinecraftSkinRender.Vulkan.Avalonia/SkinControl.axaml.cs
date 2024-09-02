using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace MinecraftSkinRender.Vulkan.Avalonia;

public partial class SkinControl : UserControl
{
    private CompositionSurfaceVisual _visual;
    private ICompositionGpuInterop _gpu;
    private Compositor _compositor;
    private readonly Action _update;
    private string _info = string.Empty;
    private bool _updateQueued;
    private bool _initialized;
    private SkinRenderVulkan _skin;

    protected CompositionDrawingSurface Surface { get; private set; }

    public SkinControl()
    {
        InitializeComponent();

        _update = UpdateFrame;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Initialize();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (_initialized)
            FreeGraphicsResources();
        _initialized = false;
        base.OnDetachedFromLogicalTree(e);
    }

    private async void Initialize()
    {
        try
        {
            var selfVisual = ElementComposition.GetElementVisual(this)!;
            _compositor = selfVisual.Compositor;

            Surface = _compositor.CreateDrawingSurface();
            _visual = _compositor.CreateSurfaceVisual();
            _visual.Size = new(Bounds.Width, Bounds.Height);
            _visual.Surface = Surface;
            ElementComposition.SetElementChildVisual(this, _visual);
            _gpu = await _compositor.TryGetCompositionGpuInterop() ?? throw new Exception("Compositor doesn't support interop for the current backend");
            Vk api = Vk.GetApi();

            _skin = new(api, this);
            _skin.VulkanInit();

            _info = GetDeviceName(api);
            _initialized = true;
            QueueNextFrame();
        }
        catch (Exception e)
        {

        }
    }

    private unsafe string GetDeviceName(Vk api)
    {
        var physicalDeviceIDProperties = new PhysicalDeviceIDProperties()
        {
            SType = StructureType.PhysicalDeviceIDProperties
        };
        var physicalDeviceProperties2 = new PhysicalDeviceProperties2()
        {
            SType = StructureType.PhysicalDeviceProperties2,
            PNext = &physicalDeviceIDProperties
        };
        api.GetPhysicalDeviceProperties2(_skin.PhysicalDevice, &physicalDeviceProperties2);

        return Marshal.PtrToStringAnsi(new IntPtr(physicalDeviceProperties2.Properties.DeviceName))!;
    }

    void UpdateFrame()
    {
        _updateQueued = false;
        var root = this.GetVisualRoot();
        if (root == null)
            return;

        _visual!.Size = new(Bounds.Width, Bounds.Height);
        var size = PixelSize.FromSize(Bounds.Size, root.RenderScaling);
        RenderFrame(size);
    }

    void QueueNextFrame()
    {
        if (_initialized && !_updateQueued && _compositor != null)
        {
            _updateQueued = true;
            _compositor?.RequestCompositionUpdate(_update);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == BoundsProperty)
            QueueNextFrame();
        base.OnPropertyChanged(change);
    }

    private void FreeGraphicsResources()
    { 
        
    }


    protected void RenderFrame(PixelSize pixelSize)
    { 
        
    }

    public IReadOnlyList<string> GetRequiredExtensions()
    {
        return _gpu.SupportedImageHandleTypes;
    }

    public SurfaceKHR CreateSurface(Instance instance)
    {
         
    }
}