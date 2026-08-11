using System.Diagnostics;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Installation;

/// <summary>启动 RA3 游戏进程（支持启动模式、附加参数与 SkuDef 配置，可选非提权启动）。</summary>
public sealed class Ra3Launcher
{
    /// <summary>按选项启动游戏。</summary>
    /// <param name="installation">RA3 安装信息。</param>
    /// <param name="options">启动选项。</param>
    /// <exception cref="FileNotFoundException">指定的 SkuDef 文件不存在时抛出。</exception>
    public void Launch(Ra3Installation installation, Ra3LaunchOptions options)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(options);
        ValidateSkudefPath(options.SkudefPath);

        var startInfo = BuildStartInfo(installation.GameExecutablePath, installation.RootPath, options);
        if (options.EnsureUnelevated)
        {
            UnelevatedProcessLauncher.Launch(startInfo);
            return;
        }

        Process.Start(startInfo);
    }

    /// <summary>使用指定 SkuDef 配置启动任意 RA3 可执行文件（-config 模式，兼容旧入口）。</summary>
    /// <param name="executablePath">游戏可执行文件路径。</param>
    /// <param name="skudefPath">SkuDef 文件路径。</param>
    /// <param name="ensureUnelevated">是否以非提权方式启动。</param>
    /// <exception cref="FileNotFoundException">可执行文件或 SkuDef 文件不存在时抛出。</exception>
    public void LaunchWithConfig(string executablePath, string skudefPath, bool ensureUnelevated = true)
    {
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("Game executable not found.", executablePath);
        }

        ValidateSkudefPath(skudefPath);

        var options = new Ra3LaunchOptions(string.Empty, EnsureUnelevated: ensureUnelevated, SkudefPath: skudefPath);
        var startInfo = BuildStartInfo(executablePath, Path.GetDirectoryName(executablePath) ?? string.Empty, options);
        if (options.EnsureUnelevated)
        {
            UnelevatedProcessLauncher.Launch(startInfo);
            return;
        }

        Process.Start(startInfo);
    }

    internal static ProcessStartInfo BuildStartInfo(string executablePath, string workingDirectory, Ra3LaunchOptions options)
    {
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

        if (!string.IsNullOrWhiteSpace(options.SkudefPath))
        {
            arguments.Add($"-config \"{options.SkudefPath}\"");
        }

        if (!string.IsNullOrWhiteSpace(options.Arguments))
        {
            arguments.Add(options.Arguments.Trim());
        }

        return new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            Arguments = string.Join(' ', arguments).Trim(),
            UseShellExecute = !options.EnsureUnelevated
        };
    }

    private static void ValidateSkudefPath(string? skudefPath)
    {
        if (skudefPath is { } path && !File.Exists(path))
        {
            throw new FileNotFoundException("Skudef file not found.", path);
        }
    }
}
