using System.Text;

namespace Ra3.Tools.Installation;

public static class Ra3CommandLine
{
    private const string GameFileMarker = "<<< DID YOU REMEMBER TO BUILD DATA??? >>>";

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

    private static IEnumerable<string> SplitArgs(string commandLine)
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
