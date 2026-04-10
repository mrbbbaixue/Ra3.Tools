using System.Diagnostics;
using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Installation;

public sealed class Ra3Launcher
{
    public void Launch(Ra3Installation installation, Ra3LaunchOptions options)
    {
        ArgumentNullException.ThrowIfNull(installation);
        ArgumentNullException.ThrowIfNull(options);

        var startInfo = BuildStartInfo(installation.GameExecutablePath, installation.RootPath, options);
        if (options.EnsureUnelevated)
        {
            UnelevatedProcessLauncher.Launch(startInfo);
            return;
        }

        Process.Start(startInfo);
    }

    public void LaunchWithConfig(string executablePath, string skudefPath, bool ensureUnelevated = true)
    {
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("Game executable not found.", executablePath);
        }

        if (!File.Exists(skudefPath))
        {
            throw new FileNotFoundException("Skudef file not found.", skudefPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = $"-config \"{skudefPath}\"",
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty,
            UseShellExecute = !ensureUnelevated
        };

        if (ensureUnelevated)
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
}
