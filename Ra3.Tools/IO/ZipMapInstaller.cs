using System.IO.Compression;

namespace Ra3.Tools.IO;

/// <summary>
/// 安装 RA3 地图 zip 包：安全解压（防 Zip Slip）后递归查找 .map 文件，
/// 平铺安装到地图根目录下的 "&lt;地图名&gt;\&lt;地图名&gt;.map" 子目录。
/// </summary>
public static class ZipMapInstaller
{
    /// <summary>
    /// 安装地图包到指定地图根目录，返回实际安装的 .map 文件完整路径列表。
    /// 解压在临时目录进行，安装完成后清理；遇到包含路径穿越（../ 或绝对路径）的条目时抛出 <see cref="InvalidDataException"/> 且不安装任何文件。
    /// </summary>
    /// <param name="zipPath">地图 zip 包路径。</param>
    /// <param name="mapsRootPath">地图根目录（如 Ra3KnownPaths.MapsDirectoryPath），不存在时自动创建。</param>
    /// <exception cref="FileNotFoundException">zip 包不存在时抛出。</exception>
    /// <exception cref="InvalidDataException">zip 内含不安全条目路径时抛出。</exception>
    public static IReadOnlyList<string> InstallMapPackage(string zipPath, string mapsRootPath)
    {
        ArgumentNullException.ThrowIfNull(zipPath);
        ArgumentNullException.ThrowIfNull(mapsRootPath);
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("Map package not found.", zipPath);
        }

        Directory.CreateDirectory(mapsRootPath);

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"Ra3Tools-{Guid.NewGuid():N}");
        try
        {
            ExtractSafely(zipPath, tempDirectory);
            return InstallMaps(tempDirectory, mapsRootPath);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static void ExtractSafely(string zipPath, string destination)
    {
        Directory.CreateDirectory(destination);
        var destinationRoot = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var targetPath = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            if (!targetPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Map package contains an unsafe entry path: {entry.FullName}");
            }

            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            entry.ExtractToFile(targetPath, overwrite: true);
        }
    }

    private static IReadOnlyList<string> InstallMaps(string extractedDirectory, string mapsRootPath)
    {
        var installed = new List<string>();
        foreach (var mapFile in Directory.EnumerateFiles(extractedDirectory, "*.map", SearchOption.AllDirectories))
        {
            var mapDirectory = Path.Combine(mapsRootPath, Path.GetFileNameWithoutExtension(mapFile));
            Directory.CreateDirectory(mapDirectory);
            var destination = Path.Combine(mapDirectory, Path.GetFileName(mapFile));
            File.Copy(mapFile, destination, overwrite: true);
            installed.Add(destination);
        }

        return installed;
    }
}
