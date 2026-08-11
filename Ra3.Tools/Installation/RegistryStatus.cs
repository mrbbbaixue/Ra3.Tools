namespace Ra3.Tools.Installation;

/// <summary>RA3 注册表状态检查结果。</summary>
public enum RegistryStatus
{
    /// <summary>注册表配置正确。</summary>
    Correct,

    /// <summary>HKLM 下缺少游戏主键。</summary>
    MissingGameKey,

    /// <summary>注册表缺少 Install Dir。</summary>
    MissingInstallPath,

    /// <summary>Install Dir 指向的不是有效安装目录。</summary>
    InvalidInstallPath,

    /// <summary>缺少产品密钥（ergc 子键）。</summary>
    MissingProductKey,

    /// <summary>未启用本地用户地图同步（UseLocalUserMaps）。</summary>
    MissingMapSync,

    /// <summary>HKCU 缺少语言设置。</summary>
    MissingCurrentUserLanguage
}
