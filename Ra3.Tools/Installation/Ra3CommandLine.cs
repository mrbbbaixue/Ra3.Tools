using System.Text;

namespace Ra3.Tools.Installation;

/// <summary>解析 RA3 进程命令行，定位游戏可执行文件与校验 RA3 特征（支持引号/转义参数拆分）。</summary>
public static class Ra3CommandLine
{
    private const string GameFileMarker = "<<< DID YOU REMEMBER TO BUILD DATA??? >>>";

    /// <summary>从命令行中提取 RA3 可执行文件路径；找不到返回 null。</summary>
    /// <param name="commandLine">进程命令行字符串。</param>
    public static string? TryExtractGameExecutablePath(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return null;
        }

        var trimmed = commandLine.TrimStart();
        foreach (var argument in SplitArgs(trimmed))
        {
            if (IsRa3LikeExecutable(argument))
            {
                return Path.GetFullPath(argument);
            }
        }

        foreach (var marker in new[] { ".game", ".exe" })
        {
            var index = trimmed.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                var candidate = trimmed[..(index + marker.Length)];
                if (IsRa3LikeExecutable(candidate))
                {
                    return Path.GetFullPath(candidate);
                }

                index = trimmed.LastIndexOf(marker, index - 1, StringComparison.OrdinalIgnoreCase);
            }
        }

        return null;
    }

    /// <summary>判断路径是否为 RA3 风格可执行文件（RA3.exe + Data\ra3_1.12.game、ra3_1.12.game，或包含游戏构建标记的文件）。</summary>
    /// <param name="path">候选路径。</param>
    public static bool IsRa3LikeExecutable(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || Directory.Exists(path))
        {
            return false;
        }

        var fileName = Path.GetFileName(path);
        if (fileName.Equals("RA3.exe", StringComparison.OrdinalIgnoreCase))
        {
            var rootPath = Path.GetDirectoryName(path);
            return rootPath != null && File.Exists(Path.Combine(rootPath, "Data", "ra3_1.12.game"));
        }

        if (fileName.Equals("ra3_1.12.game", StringComparison.OrdinalIgnoreCase))
        {
            var dataPath = Path.GetDirectoryName(path);
            var rootPath = dataPath == null ? null : Directory.GetParent(dataPath)?.FullName;
            if (rootPath != null && File.Exists(Path.Combine(rootPath, "RA3.exe")))
            {
                return true;
            }
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd().Contains(GameFileMarker, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    internal static IEnumerable<string> SplitArgs(string commandLine)
    {
        var result = new StringBuilder();
        var quoted = false;
        var escaped = false;
        var started = false;
        var allowCaret = false;

        for (var index = 0; index < commandLine.Length; index++)
        {
            var current = commandLine[index];
            if (current == '^' && !quoted)
            {
                if (allowCaret)
                {
                    result.Append(current);
                    started = true;
                    escaped = false;
                    allowCaret = false;
                }
                else if (index + 1 < commandLine.Length && commandLine[index + 1] == '^')
                {
                    allowCaret = true;
                }
                else if (index + 1 == commandLine.Length)
                {
                    result.Append(current);
                    started = true;
                    escaped = false;
                }

                continue;
            }

            if (escaped)
            {
                result.Append(current);
                started = true;
                escaped = false;
                continue;
            }

            if (current == '"')
            {
                quoted = !quoted;
                started = true;
                continue;
            }

            if (current == '\\' && index + 1 < commandLine.Length && commandLine[index + 1] == '"')
            {
                escaped = true;
                continue;
            }

            if (current == ' ' && !quoted)
            {
                if (started)
                {
                    yield return result.ToString();
                    result.Clear();
                    started = false;
                }

                continue;
            }

            result.Append(current);
            started = true;
        }

        if (started)
        {
            yield return result.ToString();
        }
    }
}
