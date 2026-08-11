using Ra3.Tools.Interop.Windows;

namespace Ra3.Tools.Installation;

/// <summary>
/// 在注册表与磁盘上定位 RA3 游戏安装目录。
/// 依次支持注册表 <c>Install Dir</c>、已知安装位置（EA / Steam 默认目录）与受限深度目录扫描，
/// 安装有效性判定与 <see cref="Ra3InstallationValidator.HasRequiredGameFiles"/> 保持一致。
/// </summary>
public static class Ra3GameLocator
{
    private const int DefaultMaxScanDepth = 4;

    /// <summary>注册表数据源（测试可替换为内存后端）。</summary>
    internal static Ra3Registry RegistrySource { get; set; } = new();

    /// <summary>已知安装位置数据源（测试可替换）。</summary>
    internal static Func<IReadOnlyList<string>> KnownLocationsSource { get; set; } = GetDefaultKnownLocations;

    /// <summary>判断并返回指定目录中的 RA3 安装；目录无效或不存在时返回 <see langword="null"/>。</summary>
    /// <param name="directory">候选安装根目录。</param>
    public static Ra3Installation? FindIn(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
        {
            return null;
        }

        return Ra3InstallationValidator.HasRequiredGameFiles(directory) ? new Ra3Installation(directory) : null;
    }

    /// <summary>在多个候选目录中返回第一个有效 RA3 安装；全部无效时返回 <see langword="null"/>。</summary>
    /// <param name="directories">候选安装根目录集合（忽略 <see langword="null"/> 与空字符串）。</param>
    public static Ra3Installation? FindIn(IEnumerable<string> directories)
    {
        ArgumentNullException.ThrowIfNull(directories);
        foreach (var directory in directories)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            var found = FindIn(directory);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>从注册表的 <c>Install Dir</c> 定位游戏安装；未设置或路径无效时返回 <see langword="null"/>。</summary>
    public static Ra3Installation? SearchRegistry()
    {
        var installPath = RegistrySource.GetInstallPath();
        return string.IsNullOrWhiteSpace(installPath) ? null : FindIn(installPath);
    }

    /// <summary>在已知安装位置（EA / Steam 默认目录）中查找；全部无效时返回 <see langword="null"/>。</summary>
    public static Ra3Installation? SearchKnownLocations()
    {
        return FindIn(KnownLocationsSource());
    }

    /// <summary>从根目录开始递归扫描（限定深度、跳过重解析点目录），返回第一个有效 RA3 安装。</summary>
    /// <param name="rootPath">扫描起始目录。</param>
    /// <param name="maxDepth">最大递归深度（0 表示只检查起始目录本身）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDepth"/> 为负数时抛出。</exception>
    public static Ra3Installation? SearchDrive(string rootPath, int maxDepth = DefaultMaxScanDepth, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);
        if (!Directory.Exists(rootPath))
        {
            return null;
        }

        return SearchDirectory(new DirectoryInfo(rootPath), 0, maxDepth, cancellationToken);
    }

    /// <summary>
    /// 聚合定位：优先注册表 <c>Install Dir</c>，其次已知安装位置，最后对全部固定盘符做受限深度扫描。
    /// 前两段命中即返回，不触发磁盘扫描。
    /// </summary>
    /// <param name="maxDriveScanDepth">磁盘扫描的最大递归深度。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static Ra3Installation? FindFirst(int maxDriveScanDepth = DefaultMaxScanDepth, CancellationToken cancellationToken = default)
    {
        return SearchRegistry()
            ?? SearchKnownLocations()
            ?? SearchAllFixedDrives(maxDriveScanDepth, cancellationToken);
    }

    private static Ra3Installation? SearchDirectory(DirectoryInfo directory, int depth, int maxDepth, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Ra3InstallationValidator.HasRequiredGameFiles(directory.FullName))
        {
            return new Ra3Installation(directory.FullName);
        }

        if (depth >= maxDepth)
        {
            return null;
        }

        foreach (var subDirectory in EnumerateSubDirectories(directory))
        {
            var found = SearchDirectory(subDirectory, depth + 1, maxDepth, cancellationToken);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static IEnumerable<DirectoryInfo> EnumerateSubDirectories(DirectoryInfo directory)
    {
        List<DirectoryInfo>? subDirectories = null;
        try
        {
            foreach (var subDirectory in directory.EnumerateDirectories())
            {
                // 跳过重解析点目录，避免 junction / 符号链接造成环或重复扫描。
                if (subDirectory.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                (subDirectories ??= new List<DirectoryInfo>()).Add(subDirectory);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            // 无权限或文件系统错误：跳过该目录，不中断扫描。
        }

        return (IEnumerable<DirectoryInfo>?)subDirectories ?? Array.Empty<DirectoryInfo>();
    }

    private static Ra3Installation? SearchAllFixedDrives(int maxDepth, CancellationToken cancellationToken)
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
            {
                continue;
            }

            var found = SearchDrive(drive.RootDirectory.FullName, maxDepth, cancellationToken);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetDefaultKnownLocations()
    {
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        var candidates = new List<string>(4);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            candidates.Add(Path.Combine(programFilesX86, "Electronic Arts", "Red Alert 3"));
            candidates.Add(Path.Combine(programFilesX86, "Steam", "steamapps", "common", "Command and Conquer Red Alert 3"));
        }

        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            candidates.Add(Path.Combine(programFiles, "Electronic Arts", "Red Alert 3"));
            candidates.Add(Path.Combine(programFiles, "Steam", "steamapps", "common", "Command and Conquer Red Alert 3"));
        }

        return candidates;
    }
}
