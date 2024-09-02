using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MinecraftSkinRender.Vulkan;

public class SkinModel
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

public class SkinPart
{
    public SkinVertex[] Vertices;
    public ushort[] Indices;
}

public class SkinDraw
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

public class SkinDrawPart
{
    public uint IndexLen;

    public Buffer VertexBuffer;
    public DeviceMemory VertexBufferMemory;
    public Buffer IndexBuffer;
    public DeviceMemory IndexBufferMemory;
}

public class SkinPartIndex
{
    public const int Head = 0;
    public const int Body = 1;
    public const int LeftArm = 2;
    public const int RightArm = 3;
    public const int LeftLeg = 4;
    public const int RightLeg = 5;
    public const int TopHead = 6;
    public const int TopBody = 7;
    public const int TopLeftArm = 8;
    public const int TopRightArm = 9;
    public const int TopLeftLeg = 10;
    public const int TopRightLeg = 11;
    public const int Cape = 12;
}