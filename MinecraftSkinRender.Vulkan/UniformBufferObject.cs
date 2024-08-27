using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;

namespace MinecraftSkinRender.Vulkan;

internal struct UniformBufferObject
{
    public Matrix4x4 model;
    public Matrix4x4 proj;
    public Matrix4x4 view;
    public Matrix4x4 self;
    public Vector3 lightColor;
}