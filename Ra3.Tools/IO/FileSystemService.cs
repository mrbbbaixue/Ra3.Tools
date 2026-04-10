using System.Diagnostics;

namespace Ra3.Tools.IO;

public static class FileSystemService
{
    public static long GetDirectorySizeBytes(string path)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        long size = 0;
        foreach (var file in Directory.EnumerateFiles(path))
        {
            size += new FileInfo(file).Length;
        }

        foreach (var directory in Directory.EnumerateDirectories(path))
        {
            size += GetDirectorySizeBytes(directory);
        }

        return size;
    }

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
