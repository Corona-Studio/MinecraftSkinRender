using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace MinecraftSkinRender.OpenGL;

public partial class SkinRenderOpenGL
{
    private const string VertexShaderSource =
@"attribute vec3 a_position;
attribute vec2 a_texCoord;
attribute vec3 a_normal;

uniform mat4 model;
uniform mat4 projection;
uniform mat4 view;
uniform mat4 self;

varying vec3 normalIn;
varying vec2 texIn;
varying vec3 fragPosIn;

void main()
{
    texIn = a_texCoord;

    mat4 temp = view * model * self;

    fragPosIn = vec3(model * vec4(a_position, 1.0));
    normalIn = normalize(vec3(model * vec4(a_normal, 1.0)));
    gl_Position = projection * temp * vec4(a_position, 1.0);
}
";

    private const string FragmentShaderSource =
@"uniform sampler2D texture0;
varying vec3 fragPosIn;
varying vec3 normalIn;
varying vec2 texIn;

void main()
{
    vec3 lightColor = vec3(1.0, 1.0, 1.0);
    float ambientStrength = 0.15;
    vec3 lightPos = vec3(0, 1, 5);
    
    vec3 ambient = ambientStrength * lightColor;

    vec3 norm = normalize(normalIn);
    vec3 lightDir = normalize(lightPos - fragPosIn);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 result = (ambient + diffuse);
    gl_FragColor = texture2D(texture0, texIn) * vec4(result, 1.0);
    //gl_FragColor = texture2D(texture0, texIn);
}
";

    private static string GetShader(Version gl, bool isgles, bool fragment, string shader)
    {
        var version = isgles ? 100 : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 150 : 120;
        var data = "#version " + version + "\n";
        if (isgles)
            data += "precision mediump float;\n";
        if (version >= 150)
        {
            shader = shader.Replace("attribute", "in");
            if (fragment)
                shader = shader
                    .Replace("varying", "in")
                    .Replace("//DECLAREGLFRAG", "out vec4 outFragColor;")
                    .Replace("gl_FragColor", "outFragColor")
                    .Replace("texture2D", "texture");
            else
                shader = shader.Replace("varying", "out");
        }

        data += shader;

        return data;
    }

    private static string GetShader(Version gl, bool isgles, bool fragment)
    {
        return GetShader(gl, isgles, fragment, fragment ? FragmentShaderSource : VertexShaderSource);
    }

    private void CreateShader()
    {
        int maj = gl.GetInteger(GetPName.MajorVersion);
        int min = gl.GetInteger(GetPName.MinorVersion);

        var vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, GetShader(new(maj, min), IsGLES, false));
        gl.CompileShader(vertexShader);
        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var state);
        if (state == 0)
        {
            gl.GetShaderInfoLog(vertexShader, out var info);
            throw new Exception($"GL_VERTEX_SHADER.\n{info}");
        }

        var fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, GetShader(new(maj, min), IsGLES, true));
        gl.CompileShader(fragmentShader);
        state = gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus);
        if (state == 0)
        {
            gl.GetShaderInfoLog(vertexShader, out var info);
            throw new Exception($"GL_FRAGMENT_SHADER.\n{info}");
        }

        _shaderProgram = gl.CreateProgram();
        gl.AttachShader(_shaderProgram, vertexShader);
        gl.AttachShader(_shaderProgram, fragmentShader);
        gl.LinkProgram(_shaderProgram);
        state = gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus);
        if (state == 0)
        {
            gl.GetProgramInfoLog(vertexShader, out var info);
            throw new Exception($"GL_LINK_PROGRAM.\n{info}");
        }

        //Delete the no longer useful individual shaders;
        gl.DetachShader(_shaderProgram, vertexShader);
        gl.DetachShader(_shaderProgram, fragmentShader);
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
    }
}
