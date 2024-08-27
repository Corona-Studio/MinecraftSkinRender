namespace MinecraftSkinRender;

/// <summary>
/// 生成史蒂夫模型
/// </summary>
public static class Steve3DModel
{
    /// <summary>
    /// 生成一个模型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public static SteveModelObj GetSteve(CubeModel model, SkinType type)
    {
        return new()
        {
            Head = new()
            {
                Model = model.GetSquare(),
                Point = model.GetSquareIndicies()
            },
            Body = new()
            {
                Model = model.GetSquare(multiplyZ: 0.5f, multiplyY: 1.5f),
                Point = model.GetSquareIndicies()
            },
            LeftArm = type == SkinType.NewSlim ? new()
            {
                Model = model.GetSquare(
                    multiplyX: 0.375f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f
                ),
                Point = model.GetSquareIndicies()
            } : new()
            {
                Model = model.GetSquare(
                    multiplyX: 0.5f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f
                ),
                Point = model.GetSquareIndicies()
            },
            RightArm = type == SkinType.NewSlim ? new()
            {
                Model = model.GetSquare(
                    multiplyX: 0.375f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f
                ),
                Point = model.GetSquareIndicies()
            } : new()
            {
                Model = model.GetSquare(
                    multiplyX: 0.5f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f
                ),
                Point = model.GetSquareIndicies()
            },
            LeftLeg = new()
            {
                Model = model.GetSquare(
                    multiplyX: 0.5f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f
                ),
                Point = model.GetSquareIndicies()
            },
            RightLeg = new()
            {
                Model = model.GetSquare(
                    multiplyX: 0.5f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f
                ),
                Point = model.GetSquareIndicies()
            },
            Cape = new()
            {
                Model = model.GetSquare(
                    multiplyX: 1.25f,
                    multiplyZ: 0.1f,
                    multiplyY: 2f
                ),
                Point = model.GetSquareIndicies()
            }
        };
    }

    /// <summary>
    /// 生成第二层模型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public static SteveModelObj GetSteveTop(CubeModel cube, SkinType type)
    {
        var model = new SteveModelObj
        {
            Head = new()
            {
                Model = cube.GetSquare(
                    enlarge: 1.125f
                ),
                Point = cube.GetSquareIndicies()
            }
        };

        if (type != SkinType.Old)
        {
            model.Body = new()
            {
                Model = cube.GetSquare(
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f,
                    enlarge: 1.125f
                ),
                Point = cube.GetSquareIndicies()
            };

            model.LeftArm = type == SkinType.NewSlim ? new()
            {
                Model = cube.GetSquare(
                    multiplyX: 0.375f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f,
                    enlarge: 1.125f
                ),
                Point = cube.GetSquareIndicies()
            } : new()
            {
                Model = cube.GetSquare(
                    multiplyX: 0.5f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f,
                    enlarge: 1.125f
                ),
                Point = cube.GetSquareIndicies()
            };

            model.RightArm = type == SkinType.NewSlim ? new()
            {
                Model = cube.GetSquare(
                    multiplyX: 0.375f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f,
                    enlarge: 1.125f
                ),
                Point = cube.GetSquareIndicies()
            } : new()
            {
                Model = cube.GetSquare(
                    multiplyX: 0.5f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f,
                    enlarge: 1.125f
                ),
                Point = cube.GetSquareIndicies()
            };

            model.LeftLeg = new()
            {
                Model = cube.GetSquare(
                    multiplyX: 0.5f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f,
                    enlarge: 1.125f
                ),
                Point = cube.GetSquareIndicies()
            };
            model.RightLeg = new()
            {
                Model = cube.GetSquare(
                    multiplyX: 0.5f,
                    multiplyZ: 0.5f,
                    multiplyY: 1.5f,
                    enlarge: 1.125f
                ),
                Point = cube.GetSquareIndicies()
            };
        }

        return model;
    }
}
