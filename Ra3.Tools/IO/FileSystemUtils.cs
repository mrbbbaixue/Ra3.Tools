using System.Diagnostics;

namespace Ra3.Tools.IO;

/// <summary>RA3 相关文件系统工具：目录大小、容量格式化、安全清理与资源管理器定位。</summary>
public static class FileSystemUtils
{
    /// <summary>递归计算目录总大小（字节）；目录不存在返回 0。不跟随重解析点（junction/符号链接），避免目录环。</summary>
    /// <param name="path">目标目录路径。</param>
    public static long GetDirectorySizeBytes(string path)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        long size = 0;
        foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos())
        {
            if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            if (entry is DirectoryInfo directory)
            {
                size += GetDirectorySizeBytes(directory.FullName);
            }
            else if (entry is FileInfo file)
            {
                size += file.Length;
            }
        }

        return size;
    }

    /// <summary>把字节数格式化为人类可读的 KB/MB/GB 字符串。</summary>
    /// <param name="size">字节数。</param>
    public static string FormatSize(long size)
    {
        if (size <= 0)
        {
            return "0B";
        }

        double value = size / 1024d;
        if (value < 1024d)
        {
            return $"{value:0.00}KB";
        }

        value /= 1024d;
        if (value < 1024d)
        {
            return $"{value:0.00}MB";
        }

        value /= 1024d;
        return $"{value:0.00}GB";
    }

    /// <summary>
    /// 递归清空目录内容（保留满足 keep 谓词的条目）。
    /// 遇到重解析点直接抛异常（拒绝删除/递归 junction），只读文件先解除只读再删除。
    /// </summary>
    /// <param name="path">目标目录路径。</param>
    /// <param name="keep">返回 true 的条目保留；默认全部删除。</param>
    public static void ClearDirectory(string path, Func<FileSystemInfo, bool>? keep = null)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        keep ??= static _ => false;
        foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos())
        {
            if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new NotSupportedException($"Attempting to delete a reparse point: {entry.FullName}");
            }

            if (entry is DirectoryInfo directory)
            {
                ClearDirectory(directory.FullName, keep);
                if (!directory.EnumerateFileSystemInfos().Any())
                {
                    directory.Delete();
                }
                continue;
            }

            if (keep(entry))
            {
                continue;
            }

            if (entry is FileInfo file)
            {
                file.IsReadOnly = false;
                file.Delete();
                continue;
            }

            throw new NotSupportedException($"Unexpected file system entry type: {entry.GetType().FullName}");
        }
    }

    /// <summary>在资源管理器中打开目标目录并选中指定文件/目录；目标不存在时无操作。</summary>
    /// <param name="path">要定位的文件或目录路径。</param>
    public static void OpenAndSelect(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{path}\"",
            UseShellExecute = true
        });
    }

    /// <summary>返回指定路径所在驱动器的可用空间（字节）；查询失败返回 null。</summary>
    /// <param name="path">任意路径。</param>
    public static long? GetAvailableFreeSpace(string path)
    {
        try
        {
            var root = Path.GetPathRoot(path) ?? path;
            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return null;
        }
    }
}
