using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftSkinRender;

/// <summary>
/// 皮肤类型
/// </summary>
public enum SkinType
{
    /// <summary>
    /// 未知的类型
    /// </summary>
    Unkonw,
    /// <summary>
    /// 1.7旧版
    /// </summary>
    Old,
    /// <summary>
    /// 1.8新版
    /// </summary>
    New,
    /// <summary>
    /// 1.8新版纤细
    /// </summary>
    NewSlim
}

public enum KeyType
{ 
    None,
    Left,
    Right
}

public enum ErrorType
{ 
    UnknowSkinType, SkinNotFind
}

public enum StateType
{ 
    SkinReload
}