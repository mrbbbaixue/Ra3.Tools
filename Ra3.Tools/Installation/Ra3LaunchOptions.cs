using Ra3.Tools.Versioning;

namespace Ra3.Tools.Installation;

/// <summary>RA3 启动选项：启动模式、附加参数与可选 SkuDef 配置文件（兼容 RA3Bar-RA3Launcher 的 -runver/-modConfig/-replayGame 语义）。</summary>
/// <param name="Arguments">附加命令行参数（原样转发给游戏进程）。</param>
/// <param name="LaunchMode">启动模式（普通 / 游戏大厅 / 窗口化）。</param>
/// <param name="EnsureUnelevated">通过 shell 以非提权方式启动（默认开启，避免继承管理员权限）。</param>
/// <param name="SkudefPath">显式指定的 SkuDef 文件路径，非空时跳过版本解析并追加 "-config" 参数。</param>
/// <param name="RunVersion">运行版本（对应 RA3Bar 的 "-runver"）：仅用于选择 SkuDef，不会转发给游戏进程（-config 已固定版本）。</param>
/// <param name="ModConfigPath">附加 MOD 的 SkuDef 文件路径，追加 "-modConfig" 参数；其中 "mod-game" 头行会覆盖 <paramref name="RunVersion"/> 参与 SkuDef 选择。</param>
/// <param name="ReplayPath">要播放的录像文件路径，追加 "-replayGame" 参数。</param>
/// <param name="LanguageName">SkuDef 的语言筛选（如 "chinese_t"、"english"）；为 null 时枚举全部语言。</param>
public sealed record Ra3LaunchOptions(string Arguments,
                                      Ra3LaunchMode LaunchMode = Ra3LaunchMode.Normal,
                                      bool EnsureUnelevated = true,
                                      string? SkudefPath = null,
                                      Ra3Version? RunVersion = null,
                                      string? ModConfigPath = null,
                                      string? ReplayPath = null,
                                      string? LanguageName = null)
{
    /// <summary>默认启动选项（无附加参数、普通模式、非提权启动）。</summary>
    public static Ra3LaunchOptions Default { get; } = new(string.Empty);
}
