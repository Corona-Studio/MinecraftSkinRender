using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftSkinRender;

public abstract class CubeModel
{
    public abstract float[] GetSquare(float multiplyX = 1.0f, float multiplyY = 1.0f, float multiplyZ = 1.0f,
        float addX = 0.0f, float addY = 0.0f, float addZ = 0.0f, float enlarge = 1.0f);

    public abstract ushort[] GetSquareIndicies(int offset = 0);
}
