namespace Ra3.Tools.Installation;

/// <summary>RA3 启动模式。</summary>
public enum Ra3LaunchMode
{
    /// <summary>普通启动。</summary>
    Normal,

    /// <summary>游戏大厅启动（-ui 参数）。</summary>
    GameBrowser,

    /// <summary>窗口化启动（-win 参数）。</summary>
    Windowed
}
