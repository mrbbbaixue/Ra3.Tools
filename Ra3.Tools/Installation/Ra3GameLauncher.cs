using System.Diagnostics;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Installation;

/// <summary>
/// 以 RA3Bar-RA3Launcher 的方式启动游戏：解析版本对应的 SkuDef（如 RA3_chinese_t_1.12.SkuDef），
/// 读取其 "set-exe" 指向的游戏本体可执行文件（如 Data\ra3_1.12.game；SkuDef 无 set-exe——如 MOD 的 SkuDef——时回退 RA3.exe），
/// 组装 "&lt;游戏本体&gt; [参数] [-modConfig ...] [-replayGame ...] -config &lt;SkuDef&gt;" 命令行并以安装根目录为工作目录启动。
/// 窗口化启动（-win）时自动执行无边框全屏位置修正（居中/定位，见 <see cref="Ra3GameWindow"/>，与 RA3Bar 一致）。
/// 不包含 RA3Bar 的 UI（控制中心、错误弹窗）；游戏退出码 123456789（无法更新）由调用方自行处理。
/// </summary>
public sealed class Ra3GameLauncher
{
    /// <summary>按选项启动游戏；非提权方式启动时（<see cref="Ra3LaunchOptions.EnsureUnelevated"/>）返回 null。</summary>
    /// <param name="installation">RA3 安装信息。</param>
    /// <param name="options">启动选项。</param>
    /// <returns>启动的游戏进程；通过 shell 非提权启动时为 null。</returns>
    /// <exception cref="FileNotFoundException">SkuDef、可执行文件、MOD 配置或显式 SkuDef 不存在时抛出。</exception>
    public Process? Launch(Ra3Installation installation, Ra3LaunchOptions options)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(options);

        var startInfo = BuildStartInfo(installation, options);
        if (options.EnsureUnelevated)
        {
            // 非提权（shell）路径拿不到进程句柄，无法做窗口修正（RA3Bar 也直接 CreateProcess）。
            UnelevatedProcessLauncher.Launch(startInfo);
            return null;
        }

        var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        // RA3Bar 无边框全屏修正：窗口化启动时等游戏窗口出现后按 -fullscreen/-xres/-yres 居中，-x/-y 显式定位。
        var adjustment = Ra3GameWindow.ParseFromArguments(options.Arguments, windowed: options.LaunchMode == Ra3LaunchMode.Windowed);
        if (adjustment is { } pending && (pending.Fullscreen || pending.X is not null || pending.Y is not null) &&
            Ra3GameWindow.TryFindWindow(process, out var windowHandle))
        {
            Ra3GameWindow.AdjustPosition(windowHandle, pending);
        }

        return process;
    }

    /// <summary>
    /// 构建启动信息但不启动进程：解析 SkuDef（显式指定或按版本/语言解析）→ 读取 set-exe（无则回退 RA3.exe）→ 组装 RA3Bar 式命令行。
    /// 可用于预览实际启动参数或手动测试（<see cref="Launch"/> 内部使用本方法）。
    /// </summary>
    /// <param name="installation">RA3 安装信息。</param>
    /// <param name="options">启动选项。</param>
    /// <exception cref="FileNotFoundException">SkuDef、可执行文件、MOD 配置或显式 SkuDef 不存在时抛出。</exception>
    public static ProcessStartInfo BuildStartInfo(Ra3Installation installation, Ra3LaunchOptions options)
    {
        // 1. 解析 SkuDef：显式指定优先；否则按版本选择（MOD 的 mod-game 头行可覆盖 RunVersion，与 RA3Bar 一致）。
        var skudefPath = options.SkudefPath;
        if (string.IsNullOrWhiteSpace(skudefPath))
        {
            var runVersion = options.RunVersion;
            if (!string.IsNullOrWhiteSpace(options.ModConfigPath))
            {
                if (!File.Exists(options.ModConfigPath))
                {
                    throw new FileNotFoundException("MOD 配置文件不存在。", options.ModConfigPath);
                }

                runVersion = Ra3SkudefCatalog.TryGetModGameVersion(options.ModConfigPath) ?? runVersion;
            }

            skudefPath = Ra3SkudefCatalog.Resolve(installation.RootPath, runVersion, options.LanguageName);
        }
        else if (!File.Exists(skudefPath))
        {
            throw new FileNotFoundException("SkuDef 文件不存在。", skudefPath);
        }

        // 2. 游戏本体：set-exe 是游戏数据 SkuDef 独有的条目（如 Data\RA3_1.12.game），MOD 的 SkuDef 一般没有。
        //    取不到 set-exe 时回退到 RA3.exe（游戏启动器自身会解析 -config 指定的 SkuDef），
        //    保证「游戏 SkuDef 启动」与「MOD SkuDef 启动」两条路径互相兼容。
        var executablePath = Ra3SkudefCatalog.TryGetGameExecutablePath(installation.RootPath, skudefPath, out var setExeExecutable)
            ? setExeExecutable!
            : installation.GameExecutablePath;
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("游戏可执行文件不存在。", executablePath);
        }

        // 3. 组装命令行（-config 置于末尾，与 RA3Bar 的 finalArguments 组装顺序一致）。
        var arguments = new List<string>();
        switch (options.LaunchMode)
        {
            case Ra3LaunchMode.GameBrowser:
                arguments.Add("-ui");
                break;
            case Ra3LaunchMode.Windowed:
                arguments.Add("-win");
                break;
            case Ra3LaunchMode.Normal:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options));
        }

        if (!string.IsNullOrWhiteSpace(options.Arguments))
        {
            arguments.Add(options.Arguments.Trim());
        }

        if (!string.IsNullOrWhiteSpace(options.ModConfigPath))
        {
            arguments.Add($"-modConfig \"{options.ModConfigPath}\"");
        }

        if (!string.IsNullOrWhiteSpace(options.ReplayPath))
        {
            arguments.Add($"-replayGame \"{options.ReplayPath}\"");
        }

        arguments.Add($"-config \"{skudefPath}\"");

        return new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = installation.RootPath,
            Arguments = string.Join(' ', arguments).Trim(),
            // 必须 CreateProcess 直启（不经 shell 文件关联）：游戏本体是 .game 扩展名，
            // 无默认关联时 ShellExecute 会弹"打开方式"对话框（RA3Bar 也是 CreateProcessW 直启）。
            // 注意：EnsureUnelevated 走 shell 非提权路径（UnelevatedProcessLauncher）时不受本字段控制。
            UseShellExecute = false
        };
    }
}
