using Ra3.Tools.Installation;
using Ra3.Tools.Versioning;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3SkudefCatalogTests
{
    [Fact]
    public void Find_EnumeratesVersionedSkudefsSortedAscending()
    {
        var installPath = CreateInstallDirectory("RA3_english_1.10.SkuDef", "RA3_english_1.13.SkuDef", "RA3_english_1.12.SkuDef");

        try
        {
            var skudefs = Ra3SkudefCatalog.Find(installPath);

            Assert.Equal(
                [Path.Combine(installPath, "RA3_english_1.10.SkuDef"),
                 Path.Combine(installPath, "RA3_english_1.12.SkuDef"),
                 Path.Combine(installPath, "RA3_english_1.13.SkuDef")],
                skudefs);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void Find_FiltersByLanguage()
    {
        var installPath = CreateInstallDirectory("RA3_english_1.12.SkuDef", "RA3_chinese_t_1.12.SkuDef");

        try
        {
            var skudefs = Ra3SkudefCatalog.Find(installPath, languageName: "chinese_t");

            Assert.Single(skudefs);
            Assert.Equal(Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef"), skudefs[0]);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void Find_IgnoresUnversionedFiles()
    {
        var installPath = CreateInstallDirectory("RA3_english.SkuDef", "RA3_english_1.12.SkuDef");

        try
        {
            var skudefs = Ra3SkudefCatalog.Find(installPath);

            Assert.Single(skudefs);
            Assert.EndsWith("RA3_english_1.12.SkuDef", skudefs[0]);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void Resolve_PicksExactRunVersion()
    {
        var installPath = CreateInstallDirectory("RA3_english_1.10.SkuDef", "RA3_english_1.12.SkuDef", "RA3_english_1.13.SkuDef");

        try
        {
            var skudef = Ra3SkudefCatalog.Resolve(installPath, Ra3Version.Parse("1.12"));

            Assert.EndsWith("RA3_english_1.12.SkuDef", skudef);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void Resolve_PicksMaxVersionWhenRunVersionNull()
    {
        var installPath = CreateInstallDirectory("RA3_english_1.10.SkuDef", "RA3_english_1.12.SkuDef", "RA3_english_1.13.SkuDef");

        try
        {
            var skudef = Ra3SkudefCatalog.Resolve(installPath);

            Assert.EndsWith("RA3_english_1.13.SkuDef", skudef);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void Resolve_SupportsSingleDigitMinorVersions()
    {
        var installPath = CreateInstallDirectory("RA3_english_1.0.SkuDef", "RA3_english_1.5.SkuDef", "RA3_english_1.9.SkuDef", "RA3_english_1.12.SkuDef");

        try
        {
            Assert.EndsWith("RA3_english_1.12.SkuDef", Ra3SkudefCatalog.Resolve(installPath));
            Assert.EndsWith("RA3_english_1.9.SkuDef", Ra3SkudefCatalog.Resolve(installPath, Ra3Version.Parse("1.09")));
            Assert.EndsWith("RA3_english_1.0.SkuDef", Ra3SkudefCatalog.Resolve(installPath, Ra3Version.Parse("1.00")));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void Resolve_ThrowsWhenRunVersionNotFound()
    {
        var installPath = CreateInstallDirectory("RA3_english_1.12.SkuDef");

        try
        {
            Assert.Throws<FileNotFoundException>(() => Ra3SkudefCatalog.Resolve(installPath, Ra3Version.Parse("1.13")));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void Resolve_ThrowsWhenNoSkudefs()
    {
        var installPath = CreateInstallDirectory();

        try
        {
            Assert.Throws<FileNotFoundException>(() => Ra3SkudefCatalog.Resolve(installPath));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void GetGameExecutablePath_ReadsSetExeRelativeToInstallRoot()
    {
        var installPath = CreateInstallDirectory();
        File.WriteAllText(Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef"), "set-exe Data\\RA3_1.12.game");
        var skudefPath = Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef");

        try
        {
            var executablePath = Ra3SkudefCatalog.GetGameExecutablePath(installPath, skudefPath);

            Assert.Equal(Path.Combine(installPath, "Data", "RA3_1.12.game"), executablePath);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void GetGameExecutablePath_ThrowsWithoutSetExeEntry()
    {
        var installPath = CreateInstallDirectory();
        File.WriteAllText(Path.Combine(installPath, "RA3_english_1.12.SkuDef"), "mod-game 1.12");
        var skudefPath = Path.Combine(installPath, "RA3_english_1.12.SkuDef");

        try
        {
            Assert.Throws<InvalidDataException>(() => Ra3SkudefCatalog.GetGameExecutablePath(installPath, skudefPath));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void GetGameExecutablePath_ThrowsWhenExecutableMissing()
    {
        var installPath = CreateInstallDirectory();
        File.WriteAllText(Path.Combine(installPath, "RA3_english_1.12.SkuDef"), "set-exe Data\\missing.exe");
        var skudefPath = Path.Combine(installPath, "RA3_english_1.12.SkuDef");

        try
        {
            var exception = Assert.Throws<FileNotFoundException>(() => Ra3SkudefCatalog.GetGameExecutablePath(installPath, skudefPath));

            Assert.EndsWith(Path.Combine("Data", "missing.exe"), exception.FileName);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void GetGameExecutablePath_ThrowsWhenSkudefMissing()
    {
        var installPath = CreateInstallDirectory();

        try
        {
            Assert.Throws<FileNotFoundException>(() => Ra3SkudefCatalog.GetGameExecutablePath(installPath, Path.Combine(installPath, "missing.skudef")));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void TryGetGameExecutablePath_ReturnsFalseWhenSetExeMissing()
    {
        var installPath = CreateInstallDirectory();
        File.WriteAllText(Path.Combine(installPath, "my mod.skudef"), "add-big Data\\Mod.big");
        var skudefPath = Path.Combine(installPath, "my mod.skudef");

        try
        {
            var found = Ra3SkudefCatalog.TryGetGameExecutablePath(installPath, skudefPath, out var executablePath);

            Assert.False(found);
            Assert.Null(executablePath);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void TryGetGameExecutablePath_ReturnsTrueWithSetExe()
    {
        var installPath = CreateInstallDirectory();
        File.WriteAllText(Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef"), "set-exe Data\\RA3_1.12.game");
        var skudefPath = Path.Combine(installPath, "RA3_chinese_t_1.12.SkuDef");

        try
        {
            var found = Ra3SkudefCatalog.TryGetGameExecutablePath(installPath, skudefPath, out var executablePath);

            Assert.True(found);
            Assert.Equal(Path.Combine(installPath, "Data", "RA3_1.12.game"), executablePath);
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void TryGetModGameVersion_ReadsModGameHeader()
    {
        var modSkudefPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.skudef");
        File.WriteAllText(modSkudefPath, "mod-game 1.13");

        try
        {
            Assert.Equal(Ra3Version.Parse("1.13"), Ra3SkudefCatalog.TryGetModGameVersion(modSkudefPath));
        }
        finally
        {
            File.Delete(modSkudefPath);
        }
    }

    [Fact]
    public void TryGetModGameVersion_ReturnsNullWhenAbsent()
    {
        var modSkudefPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.skudef");
        File.WriteAllText(modSkudefPath, "add-big Data\\Core12.big");

        try
        {
            Assert.Null(Ra3SkudefCatalog.TryGetModGameVersion(modSkudefPath));
        }
        finally
        {
            File.Delete(modSkudefPath);
        }
    }

    [Fact]
    public void TryGetModGameVersion_ReturnsNullWhenFileMissing()
    {
        Assert.Null(Ra3SkudefCatalog.TryGetModGameVersion(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.skudef")));
    }

    private static string CreateInstallDirectory(params string[] skudefFileNames)
    {
        var installPath = Path.Combine(Path.GetTempPath(), "ra3_skudef_catalog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(installPath);
        Directory.CreateDirectory(Path.Combine(installPath, "Data"));
        File.WriteAllText(Path.Combine(installPath, "RA3.exe"), string.Empty);
        File.WriteAllText(Path.Combine(installPath, "Data", "ra3_1.12.game"), string.Empty);
        foreach (var fileName in skudefFileNames)
        {
            File.WriteAllText(Path.Combine(installPath, fileName), "mod-game 1.12");
        }

        return installPath;
    }
}
