using System.IO.Compression;
using Ra3.Tools.IO;

namespace Ra3.Tools.Test.IO;

public sealed class ZipMapInstallerTests
{
    [Fact]
    public void InstallMapPackage_InstallsMapsFlatByMapName()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var zipPath = Path.Combine(root, "package.zip");
        var mapsRoot = Path.Combine(root, "Maps");
        CreateZip(zipPath,
            ("folder/sub/island.map", "map-data-1"),
            ("second.map", "map-data-2"),
            ("readme.txt", "readme"));

        try
        {
            var installed = ZipMapInstaller.InstallMapPackage(zipPath, mapsRoot);

            Assert.Equal(2, installed.Count);
            Assert.True(File.Exists(Path.Combine(mapsRoot, "island", "island.map")));
            Assert.True(File.Exists(Path.Combine(mapsRoot, "second", "second.map")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void InstallMapPackage_RejectsZipSlipEntry()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var zipPath = Path.Combine(root, "package.zip");
        var mapsRoot = Path.Combine(root, "Maps");
        CreateZip(zipPath, ("../evil.map", "map-data"));

        try
        {
            Assert.Throws<InvalidDataException>(() => ZipMapInstaller.InstallMapPackage(zipPath, mapsRoot));
            Assert.False(Directory.Exists(mapsRoot) && Directory.EnumerateFiles(mapsRoot, "*", SearchOption.AllDirectories).Any());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void InstallMapPackage_MissingPackage_Throws()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            Assert.Throws<FileNotFoundException>(() => ZipMapInstaller.InstallMapPackage(Path.Combine(root, "missing.zip"), root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void CreateZip(string zipPath, params (string Name, string Content)[] entries)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
    }
}
