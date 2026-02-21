using System.Numerics;
using SkiaSharp;

namespace MinecraftSkinRender.Image;

public static class Skin3DHeadTypeB
{
    private const int s_size = 220;
    private const int s_cubeSize = 60;
    private const int s_cubeSizeOut = s_cubeSize + (int)(s_cubeSize * 0.125);

    private static readonly SKPoint3[] s_cubeVertices =
    [
        // Inner cube
        new SKPoint3(-s_cubeSize, -s_cubeSize, -s_cubeSize), // 0
        new SKPoint3(s_cubeSize, -s_cubeSize, -s_cubeSize), // 1
        new SKPoint3(s_cubeSize, s_cubeSize, -s_cubeSize), // 2
        new SKPoint3(-s_cubeSize, s_cubeSize, -s_cubeSize), // 3
        new SKPoint3(-s_cubeSize, -s_cubeSize, s_cubeSize), // 4
        new SKPoint3(s_cubeSize, -s_cubeSize, s_cubeSize), // 5
        new SKPoint3(s_cubeSize, s_cubeSize, s_cubeSize), // 6
        new SKPoint3(-s_cubeSize, s_cubeSize, s_cubeSize), // 7
        // Outer cube (hat layer)
        new SKPoint3(-s_cubeSizeOut, -s_cubeSizeOut, -s_cubeSizeOut), // 8
        new SKPoint3(s_cubeSizeOut, -s_cubeSizeOut, -s_cubeSizeOut), // 9
        new SKPoint3(s_cubeSizeOut, s_cubeSizeOut, -s_cubeSizeOut), // 10
        new SKPoint3(-s_cubeSizeOut, s_cubeSizeOut, -s_cubeSizeOut), // 11
        new SKPoint3(-s_cubeSizeOut, -s_cubeSizeOut, s_cubeSizeOut), // 12
        new SKPoint3(s_cubeSizeOut, -s_cubeSizeOut, s_cubeSizeOut), // 13
        new SKPoint3(s_cubeSizeOut, s_cubeSizeOut, s_cubeSizeOut), // 14
        new SKPoint3(-s_cubeSizeOut, s_cubeSizeOut, s_cubeSizeOut), // 15
    ];

    private static readonly int[][] s_faces =
    [
        [8, 11, 15, 12], // Back face (Top)
        [10, 11, 15, 6], // Bottom face (Top)  -- note: mixed inner/outer, kept as-is
        [12, 13, 14, 15], // Right face (Top)
        [0, 3, 7, 4], // Back face
        [2, 3, 7, 6], // Bottom face
        [4, 5, 6, 7], // Right face
        [0, 1, 5, 4], // Top face
        [0, 1, 2, 3], // Left face
        [1, 2, 6, 5], // Front face
        [8, 9, 13, 12], // Top face (Top)
        [8, 9, 10, 11], // Left face (Top)
        [9, 10, 14, 13], // Front face (Top)
    ];

    private static readonly SKRectI[] s_facePos =
    [
        new SKRectI(56, 8, 64, 16), // Back face (Top)
        new SKRectI(48, 0, 56, 8), // Bottom face (Top)
        new SKRectI(48, 8, 56, 16), // Right face (Top)
        new SKRectI(24, 8, 32, 16), // Back face
        new SKRectI(16, 0, 24, 8), // Bottom face
        new SKRectI(16, 8, 24, 16), // Right face
        new SKRectI(8, 0, 16, 8), // Top face
        new SKRectI(0, 8, 8, 16), // Left face
        new SKRectI(8, 8, 16, 16), // Front face
        new SKRectI(40, 0, 48, 8), // Top face (Top)
        new SKRectI(32, 8, 40, 16), // Left face (Top)
        new SKRectI(40, 8, 48, 16), // Front face (Top)
    ];

    private static readonly SKPoint[][] s_sourceVertices =
    [
        [new(8, 0), new(8, 8), new(0, 8), new(0, 0)], // Back (Top)
        [new(0, 8), new(0, 0), new(8, 0), new(8, 8)], // Bottom (Top)
        [new(8, 0), new(0, 0), new(0, 8), new(8, 8)], // Right (Top)
        [new(8, 0), new(8, 8), new(0, 8), new(0, 0)], // Back
        [new(0, 8), new(0, 0), new(8, 0), new(8, 8)], // Bottom
        [new(8, 0), new(0, 0), new(0, 8), new(8, 8)], // Right
        [new(0, 0), new(0, 8), new(8, 8), new(8, 0)], // Top
        [new(0, 0), new(8, 0), new(8, 8), new(0, 8)], // Left
        [new(0, 0), new(0, 8), new(8, 8), new(8, 0)], // Front
        [new(0, 0), new(0, 8), new(8, 8), new(8, 0)], // Top (Top)
        [new(0, 0), new(8, 0), new(8, 8), new(0, 8)], // Left (Top)
        [new(0, 0), new(0, 8), new(8, 8), new(8, 0)], // Front (Top)
    ];

    public static SKImage MakeHeadImage(SKBitmap skin, int x, int y)
    {
        var info = new SKImageInfo(s_size, s_size);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        DrawHead3D(canvas, skin, x, y);
        return surface.Snapshot();
    }

    private static SKPoint TranslateToCenter(SKPoint3 p)
        => new SKPoint(p.X + s_size / 2f, p.Y + s_size / 2f);

    private static void DrawHead3D(SKCanvas canvas, SKBitmap skin, int x, int y)
    {
        // Build rotation using System.Numerics — consistent with TypeA fixes
        var rotX = Matrix4x4.CreateRotationX(x * MathF.PI / 180f);
        var rotY = Matrix4x4.CreateRotationY(y * MathF.PI / 180f);
        var pos = Matrix4x4.CreateTranslation(0, -6f, 5f);
        var combined = rotX * rotY * pos;

        // Transform all vertices
        var transformed = new SKPoint3[s_cubeVertices.Length];
        for (int i = 0; i < s_cubeVertices.Length; i++)
        {
            var v = s_cubeVertices[i];
            var r = Vector4.Transform(new Vector4(v.X, v.Y, v.Z, 1f), combined);
            transformed[i] = new SKPoint3(r.X, r.Y, r.Z);
        }

        // Perspective projection
        float perspZ = 0.001f;
        var projected = new SKPoint3[transformed.Length];
        for (int i = 0; i < transformed.Length; i++)
        {
            float z = transformed[i].Z * perspZ + 1f;
            projected[i] = new SKPoint3(
                transformed[i].X / z,
                transformed[i].Y / z,
                transformed[i].Z);
        }

        // Compute face depths (average Z of face vertices) for painter's algorithm
        var faceOrder = Enumerable.Range(0, s_faces.Length)
            .OrderBy(i => s_faces[i].Average(vi => projected[vi].Z))
            .ToArray();

        // Draw shadow using bottom face projected center
        DrawShadow(canvas, s_faces[1], projected);

        // Draw faces back-to-front
        foreach (int index in faceOrder)
        {
            var face = s_faces[index];

            var dst = new SKPoint[]
            {
                TranslateToCenter(projected[face[0]]),
                TranslateToCenter(projected[face[1]]),
                TranslateToCenter(projected[face[2]]),
                TranslateToCenter(projected[face[3]])
            };

            // Back-face culling: skip faces pointing away from viewer
            if (!IsFrontFacing(dst))
                continue;

            // Properly copy face pixels (ExtractSubset only aliases — doesn't copy)
            var faceRect = s_facePos[index];
            var sourceBitmap = new SKBitmap(8, 8, SKColorType.Rgba8888, SKAlphaType.Premul);
            using (var fc = new SKCanvas(sourceBitmap))
            {
                fc.DrawBitmap(skin,
                    new SKRect(faceRect.Left, faceRect.Top, faceRect.Right, faceRect.Bottom),
                    new SKRect(0, 0, 8, 8));
            }

            var matrix = CalculatePerspectiveTransform(s_sourceVertices[index], dst);
            canvas.SetMatrix(matrix.Matrix);

            using var paint = new SKPaint { IsAntialias = false };
            canvas.DrawBitmap(sourceBitmap, 0, 0, paint);

            sourceBitmap.Dispose();
        }

        // Reset canvas matrix
        canvas.ResetMatrix();
    }

    /// <summary>
    /// Cross-product Z of the first triangle in the quad.
    /// Positive = front-facing (counter-clockwise in screen space).
    /// </summary>
    private static bool IsFrontFacing(SKPoint[] pts)
    {
        float ax = pts[1].X - pts[0].X, ay = pts[1].Y - pts[0].Y;
        float bx = pts[2].X - pts[0].X, by = pts[2].Y - pts[0].Y;
        return (ax * by - ay * bx) < 0; // screen Y flipped, so < 0 is front-facing
    }

    private static void DrawShadow(SKCanvas canvas, int[] face, SKPoint3[] projected)
    {
        var ellipseRect = new SKRect(20, 140, 200, 180);
        using var shadowPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(0, 0, 0, 128),
            ImageFilter = SKImageFilter.CreateBlur(20, 20)
        };
        canvas.ResetMatrix();
        canvas.DrawOval(ellipseRect, shadowPaint);
    }

    private static SKMatrix44 CalculatePerspectiveTransform(SKPoint[] src, SKPoint[] dst)
    {
        float[,] mat = new float[8, 9];
        for (int i = 0; i < 4; i++)
        {
            int r = i * 2;
            mat[r, 0] = src[i].X;
            mat[r, 1] = src[i].Y;
            mat[r, 2] = 1;
            mat[r, 3] = 0;
            mat[r, 4] = 0;
            mat[r, 5] = 0;
            mat[r, 6] = -src[i].X * dst[i].X;
            mat[r, 7] = -src[i].Y * dst[i].X;
            mat[r, 8] = dst[i].X;
            r++;
            mat[r, 0] = 0;
            mat[r, 1] = 0;
            mat[r, 2] = 0;
            mat[r, 3] = src[i].X;
            mat[r, 4] = src[i].Y;
            mat[r, 5] = 1;
            mat[r, 6] = -src[i].X * dst[i].Y;
            mat[r, 7] = -src[i].Y * dst[i].Y;
            mat[r, 8] = dst[i].Y;
        }

        // Gaussian elimination
        for (int i = 0; i < 8; i++)
        {
            int maxRow = i;
            for (int k = i + 1; k < 8; k++)
                if (MathF.Abs(mat[k, i]) > MathF.Abs(mat[maxRow, i]))
                    maxRow = k;
            for (int k = i; k < 9; k++) (mat[i, k], mat[maxRow, k]) = (mat[maxRow, k], mat[i, k]);
            float pivot = mat[i, i];
            for (int k = i; k < 9; k++) mat[i, k] /= pivot;
            for (int k = 0; k < 8; k++)
            {
                if (k == i) continue;
                float f = mat[k, i];
                for (int j = i; j < 9; j++) mat[k, j] -= f * mat[i, j];
            }
        }

        float[] h = new float[9];
        for (int i = 0; i < 8; i++) h[i] = mat[i, 8];
        h[8] = 1;

        var m = new SKMatrix44();
        m[0, 0] = h[0];
        m[0, 1] = h[1];
        m[0, 2] = 0;
        m[0, 3] = h[2];
        m[1, 0] = h[3];
        m[1, 1] = h[4];
        m[1, 2] = 0;
        m[1, 3] = h[5];
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = 1;
        m[2, 3] = 0;
        m[3, 0] = h[6];
        m[3, 1] = h[7];
        m[3, 2] = 0;
        m[3, 3] = h[8];
        return m;
    }
}