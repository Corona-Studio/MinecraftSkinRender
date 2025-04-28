using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftSkinRender.OpenGL;

public partial class OpenGLFXAA
{
    private int _fxaaVAO;
    private int _fxaaVBO;

    private readonly float[] _quadVertices = 
    [
        -1.0f,  1.0f,   0.0f, 1.0f,
        -1.0f, -1.0f,   0.0f, 0.0f,
         1.0f,  1.0f,   1.0f, 1.0f,
         1.0f, -1.0f,   1.0f, 0.0f
    ];

    private unsafe void InitFXAA()
    {
        gl.UseProgram(_shaderFXAAProgram);

        _fxaaVAO = gl.GenVertexArray();
        gl.BindVertexArray(_fxaaVAO);

        _fxaaVBO = gl.GenBuffer();
        gl.BindBuffer(gl.GL_ARRAY_BUFFER, _fxaaVBO);
        fixed (void* ptr = _quadVertices)
        {
            gl.BufferData(gl.GL_ARRAY_BUFFER, _quadVertices.Length * sizeof(float), new(ptr), gl.GL_STATIC_DRAW);
        }

        int posLoc = gl.GetAttribLocation(_shaderFXAAProgram, "a_position");
        int texLoc = gl.GetAttribLocation(_shaderFXAAProgram, "a_texCoord");

        gl.EnableVertexAttribArray(posLoc);
        gl.VertexAttribPointer(posLoc, 2, gl.GL_FLOAT, false, 4 * sizeof(float), 0);

        gl.EnableVertexAttribArray(texLoc);
        gl.VertexAttribPointer(texLoc, 2, gl.GL_FLOAT, false, 4 * sizeof(float), 2 * sizeof(float));

        gl.BindVertexArray(0);
        gl.UseProgram(0);
    }

    private void DeleteFXAA()
    {
        if (_fxaaVAO != 0)
        {
            gl.DeleteVertexArray(_fxaaVAO);
            _fxaaVAO = 0;
        }
        if (_fxaaVBO != 0)
        {
            gl.DeleteBuffer(_fxaaVBO);
            _fxaaVBO = 0;
        }
    }
}
