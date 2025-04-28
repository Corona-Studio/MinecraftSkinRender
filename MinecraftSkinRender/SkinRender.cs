using System.Numerics;
using SkiaSharp;

namespace MinecraftSkinRender;

public abstract class SkinRender
{
    protected bool _switchModel = false;
    protected bool _switchSkin = false;
    protected bool _switchType = false;
    protected bool _switchBack = false;
    protected bool _enableAnimation;
    protected double _time;

    protected float _dis = 1;

    protected Vector2 _rotXY;
    protected Vector2 _diffXY;

    protected Vector2 _xy;
    protected Vector2 _saveXY;
    protected Vector2 _lastXY;

    protected Matrix4x4 _last;

    protected readonly SkinAnimation _skina;

    public event Action<object?, ErrorType>? Error;
    public event Action<object?, StateType>? State;

    public int Width { get; set; }
    public int Height { get; set; }

    public string Info { get; protected set; }
    public SKBitmap? Skin { get; private set; }
    public SKBitmap? Cape { get; private set; }
    public SkinType SkinType { get; protected set; } = SkinType.Unkonw;
    public bool HaveCape { get; protected set; }
    public bool HaveSkin { get; protected set; }
    public Vector4 BackColor { get; protected set; }
    public SkinRenderType RenderType { get; protected set; }

    public bool EnableCape { get; private set; }
    public bool EnableTop { get; private set; }

    public Vector3 ArmRotate { get; set; }
    public Vector3 LegRotate { get; set; }
    public Vector3 HeadRotate { get; set; }

    public int Fps { get; private set; }
    public event Action<object?, int>? FpsUpdate;

    public SkinRender()
    {
        _skina = new();
        _last = Matrix4x4.Identity;
    }

    public void SetBackColor(Vector4 color)
    {
        BackColor = color;
        _switchBack = true;
    }

    public void SetRenderType(SkinRenderType type)
    {
        RenderType = type;
        _switchType = true;
    }

    public void SetCape(bool enable)
    {
        EnableCape = enable;
        _switchType = true;
    }

    public void SetTopModel(bool top)
    {
        EnableTop = top;
        _switchType = true;
    }

    public void SetAnimation(bool enable)
    {
        if (enable)
        {
            _skina.Run = true;
        }
        else
        {
            _skina.Run = false;
        }

        _enableAnimation = enable;
    }

    public void PointerPressed(KeyType type, Vector2 point)
    {
        if (type == KeyType.Left)
        {
            _diffXY.X = point.X;
            _diffXY.Y = -point.Y;
        }
        else if (type == KeyType.Right)
        {
            _lastXY.X = point.X;
            _lastXY.Y = point.Y;
        }
    }

    public void PointerReleased(KeyType type, Vector2 point)
    {
        if (type == KeyType.Right)
        {
            _saveXY.X = _xy.X;
            _saveXY.Y = _xy.Y;
        }
    }

    public void PointerMoved(KeyType type, Vector2 point)
    {
        if (type == KeyType.Left)
        {
            _rotXY.Y = point.X - _diffXY.X;
            _rotXY.X = point.Y + _diffXY.Y;
            _rotXY.Y *= 2;
            _rotXY.X *= 2;
            _diffXY.X = point.X;
            _diffXY.Y = -point.Y;
        }
        else if (type == KeyType.Right)
        {
            _xy.X = -(_lastXY.X - point.X) / 100 + _saveXY.X;
            _xy.Y = (_lastXY.Y - point.Y) / 100 + _saveXY.Y;
        }
    }

    public void PointerWheelChanged(bool ispost)
    {
        if (ispost)
        {
            _dis += 0.1f;
        }
        else
        {
            _dis -= 0.1f;
        }
    }

    public void Rot(float x, float y)
    {
        _rotXY.X += x;
        _rotXY.Y += y;
    }

    public void Pos(float x, float y)
    {
        _xy.X += x;
        _xy.Y += y;
    }

    public void AddDis(float x)
    {
        _dis += x;
    }

    public void SetSkinType(SkinType type)
    {
        if (type == SkinType)
        {
            return;
        }
        SkinType = type;
        _skina.SkinType = type;
        _switchModel = true;
    }

    public static SKBitmap MakeImage(SKBitmap image)
    {
        var image2 = new SKBitmap(256, 256);

        int sec = 256 / image.Width;

        for (int i = 0; i < image.Width; i++)
        {
            int ax = sec * i;
            for (int j = 0; j < image.Height; j++)
            {
                int bx = sec * j;
                var color = image.GetPixel(i, j);

                for (int a = 0; a < sec; a++)
                {
                    for (int b = 0; b < sec; b++)
                    {
                        image2.SetPixel(a + ax, b + bx, color);
                    }
                }
            }
        }

        return image2;
    }

    public void SetSkin(SKBitmap? skin)
    {
        Skin?.Dispose();
        if (skin == null)
        {
            HaveSkin = false;
            return;
        }
        if (skin.Width != 64)
        {
            throw new Exception("This is not skin image");
        }

        Skin = skin;

        SetSkinType(SkinTypeChecker.GetTextType(skin));
        _switchSkin = true;
        HaveSkin = true;
    }

    public void SetCape(SKBitmap? cape)
    {
        Cape = cape;
        if (cape == null)
        {
            HaveCape = false;
            return;
        }
        _switchSkin = true;
        HaveCape = true;
    }

    public void ResetPos()
    {
        _dis = 1;
        _diffXY.X = 0;
        _diffXY.Y = 0;
        _xy.X = 0;
        _xy.Y = 0;
        _saveXY.X = 0;
        _saveXY.Y = 0;
        _lastXY.X = 0;
        _lastXY.Y = 0;
        _last = Matrix4x4.Identity;
    }

    public void Tick(double time)
    {
        if (_enableAnimation)
        {
            _skina.Tick(time);
        }

        if (_rotXY.X != 0 || _rotXY.Y != 0)
        {
            _last *= Matrix4x4.CreateRotationX(_rotXY.X / 360)
                    * Matrix4x4.CreateRotationY(_rotXY.Y / 360);
            _rotXY.X = 0;
            _rotXY.Y = 0;
        }

        Fps++;
        _time += time;
        if (_time > 1)
        {
            _time -= 1;
            FpsUpdate?.Invoke(this, Fps);
            Fps = 0;
        }
    }

    protected void OnErrorChange(ErrorType data)
    {
        Error?.Invoke(this, data);
    }

    protected void OnStateChange(StateType data)
    {
        State?.Invoke(this, data);
    }

    protected Matrix4x4 GetMatrix4(MatrPartType type)
    {
        var value = SkinType == SkinType.NewSlim ? 1.375f : 1.5f;
        bool enable = _enableAnimation;

        return type switch
        {
            MatrPartType.Head => Matrix4x4.CreateTranslation(0, CubeModel.Value, 0) *
              Matrix4x4.CreateRotationZ((enable ? _skina.Head.X : HeadRotate.X) / 360) *
              Matrix4x4.CreateRotationX((enable ? _skina.Head.Y : HeadRotate.Y) / 360) *
              Matrix4x4.CreateRotationY((enable ? _skina.Head.Z : HeadRotate.Z) / 360) *
              Matrix4x4.CreateTranslation(0, CubeModel.Value * 1.5f, 0),
            MatrPartType.LeftArm => Matrix4x4.CreateTranslation(CubeModel.Value / 2, -(value * CubeModel.Value), 0) *
               Matrix4x4.CreateRotationZ((enable ? _skina.Arm.X : ArmRotate.X) / 360) *
               Matrix4x4.CreateRotationX((enable ? _skina.Arm.Y : ArmRotate.Y) / 360) *
               Matrix4x4.CreateTranslation(value * CubeModel.Value - CubeModel.Value / 2, value * CubeModel.Value, 0),
            MatrPartType.RightArm => Matrix4x4.CreateTranslation(-CubeModel.Value / 2, -(value * CubeModel.Value), 0) *
                  Matrix4x4.CreateRotationZ((enable ? -_skina.Arm.X : -ArmRotate.X) / 360) *
                  Matrix4x4.CreateRotationX((enable ? -_skina.Arm.Y : -ArmRotate.Y) / 360) *
                  Matrix4x4.CreateTranslation(
                      -value * CubeModel.Value + CubeModel.Value / 2, value * CubeModel.Value, 0),
            MatrPartType.LeftLeg => Matrix4x4.CreateTranslation(0, -1.5f * CubeModel.Value, 0) *
               Matrix4x4.CreateRotationZ((enable ? _skina.Leg.X : LegRotate.X) / 360) *
               Matrix4x4.CreateRotationX((enable ? _skina.Leg.Y : LegRotate.Y) / 360) *
               Matrix4x4.CreateTranslation(CubeModel.Value * 0.5f, -CubeModel.Value * 1.5f, 0),
            MatrPartType.RightLeg => Matrix4x4.CreateTranslation(0, -1.5f * CubeModel.Value, 0) *
            Matrix4x4.CreateRotationZ((enable ? -_skina.Leg.X : -LegRotate.X) / 360) *
            Matrix4x4.CreateRotationX((enable ? -_skina.Leg.Y : -LegRotate.Y) / 360) *
            Matrix4x4.CreateTranslation(-CubeModel.Value * 0.5f, -CubeModel.Value * 1.5f, 0),
            MatrPartType.Proj => Matrix4x4.CreatePerspectiveFieldOfView(
              (float)(Math.PI / 4), (float)Width / Height, 0.1f, 10.0f),
            MatrPartType.View => Matrix4x4.CreateLookAt(new(0, 0, 7), new(), new(0, 1, 0)),
            MatrPartType.Model => _last
            * Matrix4x4.CreateTranslation(new(_xy.X, _xy.Y, 0))
            * Matrix4x4.CreateScale(_dis),
            MatrPartType.Cape => Matrix4x4.CreateTranslation(0, -2f * CubeModel.Value, -CubeModel.Value * 0.1f) *
               Matrix4x4.CreateRotationX((float)((enable ? 11.8 + _skina.Cape : 6.3) * Math.PI / 180)) *
               Matrix4x4.CreateTranslation(0, 1.6f * CubeModel.Value, -CubeModel.Value * 0.5f),
            _ => Matrix4x4.Identity
        };
    }
}
