using Ra3.Tools.IO;

namespace Ra3.Tools.Test.IO;

public sealed class FileSystemServiceTests
{
    [Fact]
    public async Task SharedFile_ReadAllBytesAsync_ReadsOpenFile()
    {
        var tempPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempPath, "hello");

        try
        {
            using var writeHandle = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            var bytes = await SharedFile.ReadAllBytesAsync(tempPath);
            Assert.Equal("hello", System.Text.Encoding.UTF8.GetString(bytes));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void ClearDirectory_RespectsKeepPredicate()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(root, "delete.txt"), "delete");

        try
        {
            FileSystemService.ClearDirectory(root, info => info.Name == "keep.txt");
            Assert.True(File.Exists(Path.Combine(root, "keep.txt")));
            Assert.False(File.Exists(Path.Combine(root, "delete.txt")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FormatSize_ReturnsHumanReadableValue()
    {
        Assert.Equal("1.00KB", FileSystemService.FormatSize(1024));
    }
}
