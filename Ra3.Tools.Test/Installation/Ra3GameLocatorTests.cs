using System.Diagnostics;
using Ra3.Tools.Installation;
using Ra3.Tools.Test.TestDoubles;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3GameLocatorTests
{
    [Fact]
    public void FindIn_ReturnsInstallationForValidDirectory()
    {
        var installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true);

        try
        {
            var installation = Ra3GameLocator.FindIn(installPath);

            Assert.NotNull(installation);
            Assert.Equal(Path.GetFullPath(installPath), installation.RootPath);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void FindIn_ReturnsNullForInvalidDirectory()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "ra3_locator_invalid", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        try
        {
            Assert.Null(Ra3GameLocator.FindIn(directoryPath));
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void FindIn_ReturnsNullForMissingDirectory()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "ra3_locator_missing", Guid.NewGuid().ToString("N"));

        Assert.Null(Ra3GameLocator.FindIn(directoryPath));
    }

    [Fact]
    public void FindIn_ReturnsFirstValidAmongCandidates()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), "ra3_locator_candidates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(invalidPath);
        var installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true);

        try
        {
            var installation = Ra3GameLocator.FindIn(new[] { invalidPath, installPath });

            Assert.NotNull(installation);
            Assert.Equal(Path.GetFullPath(installPath), installation.RootPath);
        }
        finally
        {
            Directory.Delete(invalidPath, recursive: true);
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void FindIn_ReturnsNullWhenAllCandidatesInvalid()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), "ra3_locator_all_invalid", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(firstPath);

        try
        {
            Assert.Null(Ra3GameLocator.FindIn(new[] { firstPath, string.Empty, @"C:\non-existent" }));
        }
        finally
        {
            Directory.Delete(firstPath, recursive: true);
        }
    }

    [Fact]
    public void SearchRegistry_ReturnsInstallationFromRegistry()
    {
        var installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true);
        var registry = new Ra3Registry(new MemoryRegistryBackend());
        registry.SetInstallPath(installPath);
        var original = Ra3GameLocator.RegistrySource;

        try
        {
            Ra3GameLocator.RegistrySource = registry;

            var installation = Ra3GameLocator.SearchRegistry();

            Assert.NotNull(installation);
            Assert.Equal(Path.GetFullPath(installPath), installation.RootPath);
        }
        finally
        {
            Ra3GameLocator.RegistrySource = original;
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void SearchRegistry_ReturnsNullWhenRegistryPathMissing()
    {
        var registry = new Ra3Registry(new MemoryRegistryBackend());
        var original = Ra3GameLocator.RegistrySource;

        try
        {
            Ra3GameLocator.RegistrySource = registry;

            Assert.Null(Ra3GameLocator.SearchRegistry());
        }
        finally
        {
            Ra3GameLocator.RegistrySource = original;
        }
    }

    [Fact]
    public void SearchRegistry_ReturnsNullWhenRegistryPathInvalid()
    {
        var registry = new Ra3Registry(new MemoryRegistryBackend());
        registry.SetInstallPath(@"C:\non-existent\Red Alert 3");
        var original = Ra3GameLocator.RegistrySource;

        try
        {
            Ra3GameLocator.RegistrySource = registry;

            Assert.Null(Ra3GameLocator.SearchRegistry());
        }
        finally
        {
            Ra3GameLocator.RegistrySource = original;
        }
    }

    [Fact]
    public void SearchKnownLocations_ReturnsInstallationFromKnownLocation()
    {
        var installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true);
        var original = Ra3GameLocator.KnownLocationsSource;

        try
        {
            Ra3GameLocator.KnownLocationsSource = () => new[] { installPath, @"C:\non-existent" };

            var installation = Ra3GameLocator.SearchKnownLocations();

            Assert.NotNull(installation);
            Assert.Equal(Path.GetFullPath(installPath), installation.RootPath);
        }
        finally
        {
            Ra3GameLocator.KnownLocationsSource = original;
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void SearchKnownLocations_ReturnsNullWhenNoKnownLocationValid()
    {
        var original = Ra3GameLocator.KnownLocationsSource;

        try
        {
            Ra3GameLocator.KnownLocationsSource = () => new[] { @"C:\non-existent" };

            Assert.Null(Ra3GameLocator.SearchKnownLocations());
        }
        finally
        {
            Ra3GameLocator.KnownLocationsSource = original;
        }
    }

    [Fact]
    public void SearchDrive_FindsInstallAtRootWithZeroDepth()
    {
        var installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true);

        try
        {
            var installation = Ra3GameLocator.SearchDrive(installPath, maxDepth: 0);

            Assert.NotNull(installation);
            Assert.Equal(Path.GetFullPath(installPath), installation.RootPath);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void SearchDrive_FindsNestedInstallWithinMaxDepth()
    {
        var rootPath = CreateNestedTree(installDepth: 2, out var installPath);

        try
        {
            var installation = Ra3GameLocator.SearchDrive(rootPath, maxDepth: 3);

            Assert.NotNull(installation);
            Assert.Equal(Path.GetFullPath(installPath), installation.RootPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void SearchDrive_ReturnsNullWhenInstallBeyondMaxDepth()
    {
        var rootPath = CreateNestedTree(installDepth: 3, out _);

        try
        {
            Assert.Null(Ra3GameLocator.SearchDrive(rootPath, maxDepth: 3));
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void SearchDrive_ReturnsNullForMissingRoot()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "ra3_locator_missing_root", Guid.NewGuid().ToString("N"));

        Assert.Null(Ra3GameLocator.SearchDrive(rootPath));
    }

    [Fact]
    public void SearchDrive_ThrowsForNegativeMaxDepth()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "ra3_locator_negative", Guid.NewGuid().ToString("N"));

        Assert.Throws<ArgumentOutOfRangeException>(() => Ra3GameLocator.SearchDrive(rootPath, maxDepth: -1));
    }

    [Fact]
    public void SearchDrive_SkipsReparsePointDirectories()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "ra3_locator_junction", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        var targetPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true, baseFolderName: "ra3_junction_target");
        var junctionPath = Path.Combine(rootPath, "junction");
        if (!TryCreateJunction(junctionPath, targetPath))
        {
            // 环境不支持创建 junction 时跳过本测试。
            Directory.Delete(targetPath, recursive: true);
            Directory.Delete(rootPath, recursive: true);
            return;
        }

        try
        {
            // 有效安装位于 junction 目标内：若跟随重解析点会被找到，跳过则返回 null。
            Assert.Null(Ra3GameLocator.SearchDrive(rootPath, maxDepth: 2));
        }
        finally
        {
            Directory.Delete(targetPath, recursive: true);
            // .NET 递归删除对 junction 子项会拒绝访问：先显式删除链接本身。
            if (Directory.Exists(junctionPath))
            {
                Directory.Delete(junctionPath);
            }

            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void FindFirst_PrefersRegistryOverKnownLocations()
    {
        var registryPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true, baseFolderName: "ra3_registry_install");
        var knownPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true, baseFolderName: "ra3_known_install");
        var registry = new Ra3Registry(new MemoryRegistryBackend());
        registry.SetInstallPath(registryPath);
        var originalRegistry = Ra3GameLocator.RegistrySource;
        var originalKnown = Ra3GameLocator.KnownLocationsSource;

        try
        {
            Ra3GameLocator.RegistrySource = registry;
            Ra3GameLocator.KnownLocationsSource = () => new[] { knownPath };

            var installation = Ra3GameLocator.FindFirst();

            Assert.NotNull(installation);
            Assert.Equal(Path.GetFullPath(registryPath), installation.RootPath);
        }
        finally
        {
            Ra3GameLocator.RegistrySource = originalRegistry;
            Ra3GameLocator.KnownLocationsSource = originalKnown;
            Directory.Delete(registryPath, recursive: true);
            Directory.Delete(knownPath, recursive: true);
        }
    }

    [Fact]
    public void FindFirst_FallsBackToKnownLocationsWhenRegistryIsEmpty()
    {
        var knownPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true, baseFolderName: "ra3_fallback_install");
        var registry = new Ra3Registry(new MemoryRegistryBackend());
        var originalRegistry = Ra3GameLocator.RegistrySource;
        var originalKnown = Ra3GameLocator.KnownLocationsSource;

        try
        {
            Ra3GameLocator.RegistrySource = registry;
            Ra3GameLocator.KnownLocationsSource = () => new[] { knownPath };

            var installation = Ra3GameLocator.FindFirst();

            Assert.NotNull(installation);
            Assert.Equal(Path.GetFullPath(knownPath), installation.RootPath);
        }
        finally
        {
            Ra3GameLocator.RegistrySource = originalRegistry;
            Ra3GameLocator.KnownLocationsSource = originalKnown;
            Directory.Delete(knownPath, recursive: true);
        }
    }

    private static bool TryCreateJunction(string junctionPath, string targetPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junctionPath}\" \"{targetPath}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var process = Process.Start(startInfo)!;
            process.WaitForExit();
            return process.ExitCode == 0 && Directory.Exists(junctionPath);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private static string CreateNestedTree(int installDepth, out string installPath)
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "ra3_locator_tree", Guid.NewGuid().ToString("N"));
        var current = rootPath;
        for (var i = 0; i < installDepth; i++)
        {
            current = Path.Combine(current, $"level{i}");
            Directory.CreateDirectory(current);
        }

        installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true, baseFolderName: "ra3_nested_install");
        Directory.Move(installPath, Path.Combine(current, "install"));
        installPath = Path.Combine(current, "install");
        return rootPath;
    }
}
