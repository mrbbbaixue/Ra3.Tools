namespace Ra3.Tools.Installation;

/// <summary>RA3 启动选项：启动模式、附加参数与可选 SkuDef 配置文件。</summary>
/// <param name="Arguments">附加命令行参数。</param>
/// <param name="LaunchMode">启动模式（普通 / 游戏大厅 / 窗口化）。</param>
/// <param name="EnsureUnelevated">通过 shell 以非提权方式启动（默认开启，避免继承管理员权限）。</param>
/// <param name="SkudefPath">可选的 SkuDef 文件路径，非空时追加 "-config" 参数。</param>
public sealed record Ra3LaunchOptions(string Arguments,
                                      Ra3LaunchMode LaunchMode = Ra3LaunchMode.Normal,
                                      bool EnsureUnelevated = true,
                                      string? SkudefPath = null)
{
    /// <summary>默认启动选项（无附加参数、普通模式、非提权启动）。</summary>
    public static Ra3LaunchOptions Default { get; } = new(string.Empty);
}
