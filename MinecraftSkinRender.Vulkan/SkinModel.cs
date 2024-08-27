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
    public SkinPart Head;
    public SkinPart Body;
    public SkinPart LeftArm;
    public SkinPart RightArm;
    public SkinPart LeftLeg;
    public SkinPart RightLeg;

    public SkinPart TopHead;
    public SkinPart TopBody;
    public SkinPart TopLeftArm;
    public SkinPart TopRightArm;
    public SkinPart TopLeftLeg;
    public SkinPart TopRightLeg;

    public SkinPart Cape;
}

internal class SkinPart
{
    public SkinVertex[] Vertices;
    public ushort[] Indices;
}

internal class SkinDraw
{
    public SkinDrawPart Head;
    public SkinDrawPart Body;
    public SkinDrawPart LeftArm;
    public SkinDrawPart RightArm;
    public SkinDrawPart LeftLeg;
    public SkinDrawPart RightLeg;

    public SkinDrawPart TopHead;
    public SkinDrawPart TopBody;
    public SkinDrawPart TopLeftArm;
    public SkinDrawPart TopRightArm;
    public SkinDrawPart TopLeftLeg;
    public SkinDrawPart TopRightLeg;

    public SkinDrawPart Cape;
}

internal class SkinDrawPart
{
    public Buffer VertexBuffer;
    public DeviceMemory VertexBufferMemory;
    public Buffer IndexBuffer;
    public DeviceMemory IndexBufferMemory;
}