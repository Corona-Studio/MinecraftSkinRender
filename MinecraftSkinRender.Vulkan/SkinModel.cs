using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MinecraftSkinRender.Vulkan;

internal class SkinModel
{
    public SkinPart Head = new();
    public SkinPart Body = new();
    public SkinPart LeftArm = new();
    public SkinPart RightArm = new();
    public SkinPart LeftLeg = new();
    public SkinPart RightLeg = new();

    public SkinPart TopHead = new();
    public SkinPart TopBody = new();
    public SkinPart TopLeftArm = new();
    public SkinPart TopRightArm = new();
    public SkinPart TopLeftLeg = new();
    public SkinPart TopRightLeg = new();

    public SkinPart Cape = new();
}

internal class SkinPart
{
    public SkinVertex[] Vertices;
    public ushort[] Indices;
}

internal class SkinDraw
{
    public SkinDrawPart Head = new();
    public SkinDrawPart Body = new();
    public SkinDrawPart LeftArm = new();
    public SkinDrawPart RightArm = new();
    public SkinDrawPart LeftLeg = new();
    public SkinDrawPart RightLeg = new();

    public SkinDrawPart TopHead = new();
    public SkinDrawPart TopBody = new();
    public SkinDrawPart TopLeftArm = new();
    public SkinDrawPart TopRightArm = new();
    public SkinDrawPart TopLeftLeg = new();
    public SkinDrawPart TopRightLeg = new();

    public SkinDrawPart Cape = new();
}

internal class SkinDrawPart
{
    public uint IndexLen;

    public Buffer VertexBuffer;
    public DeviceMemory VertexBufferMemory;
    public Buffer IndexBuffer;
    public DeviceMemory IndexBufferMemory;

    public Buffer[] uniformBuffers;
    public DeviceMemory[] uniformBuffersMemory;

    public DescriptorPool descriptorPool;
    public DescriptorSet[] descriptorSets;
}