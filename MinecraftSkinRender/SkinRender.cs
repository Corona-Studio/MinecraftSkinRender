using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace MinecraftSkinRender;

public abstract class SkinRender
{
    protected bool _switchModel = false;
    protected bool _switchSkin = false;
    protected bool _enableAnimation;

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
    public bool HaveCape { get; private set; }
    public bool HaveSkin { get; private set; }
    public bool IsLoading { get; protected set; }

    public bool EnableCape { get; set; }
    public bool IsGLES { get; set; }
    public bool EnableMSAA { get; set; }
    public bool EnableTop { get; set; }

    public Vector3 ArmRotate { get; set; }
    public Vector3 LegRotate { get; set; }
    public Vector3 HeadRotate { get; set; }

    public SkinRender()
    {
        _skina = new();
        _last = Matrix4x4.Identity;
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

    public void SetSkin(SKBitmap? skin)
    {
        Skin = skin;
        if (skin == null)
        {
            HaveSkin = false;
            return;
        }
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

    protected void OnErrorChange(ErrorType data)
    {
        Error?.Invoke(this, data);
    }

    protected void OnStateChange(StateType data)
    {
        State?.Invoke(this, data);
    }
}
