using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace MinecraftSkinRender.Vulkan;

internal struct SkinVertex
{
    /// <summary>
    /// 顶点
    /// </summary>
    public Vector3D<float> Pos;
    /// <summary>
    /// 光照用
    /// </summary>
    public Vector3D<float> Normal;
    /// <summary>
    /// 贴图UV
    /// </summary>
    public Vector2D<float> TextCoord;

    public static VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<SkinVertex>(),
            InputRate = VertexInputRate.Vertex,
        };

        return bindingDescription;
    }

    public static VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        var attributeDescriptions = new[]
        {
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<SkinVertex>(nameof(Pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<SkinVertex>(nameof(Normal)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<SkinVertex>(nameof(TextCoord)),
            }
        };

        return attributeDescriptions;
    }
}
