using System.Numerics;

namespace MinecraftSkinRender;

/// <summary>
/// 皮肤的动画
/// </summary>
public class SkinAnimation
{
    private int _frame = 0;
    private bool _close = false;

    public bool Run { get; set; }
    public SkinType SkinType { get; set; }

    public Vector3 Arm;
    public Vector3 Leg;
    public Vector3 Head;

    public SkinAnimation()
    {
        Arm.X = 40;
    }

    public void Close()
    {
        Run = false;
        _close = true;
    }

    /// <summary>
    /// 进行动画演算
    /// </summary>
    public bool Tick(double time)
    {
        if (Run)
        {
            time *= 1000;
            _frame+= (int)time;
            if (_frame > 1200)
            {
                _frame = 0;
            }

            int down = _frame / 10;

            if (down <= 60)
            {
                //0 360
                //-180 180
                Arm.Y = down * 6 - 180;
                //0 180
                //90 -90
                Leg.Y = 90 - down * 3;
                //-30 30
                if (SkinType == SkinType.NewSlim)
                {
                    Head.Z = 0;
                    Head.X = down - 30;
                }
                else
                {
                    Head.X = 0;
                    Head.Z = down - 30;
                }
            }
            else
            {
                //360 720
                //180 -180
                Arm.Y = 540 - down * 6;
                //180 360
                //-90 90
                Leg.Y = down * 3 - 270;
                //30 -30
                if (SkinType == SkinType.NewSlim)
                {
                    Head.Z = 0;
                    Head.X = 90 - down;
                }
                else
                {
                    Head.X = 0;
                    Head.Z = 90 - down;
                }
            }
        }

        return !_close;
    }
}