using System.Diagnostics;
using Ra3.Tools.IO;

namespace Ra3.Tools.Test.IO;

public sealed class FileSystemUtilsTests
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
            FileSystemUtils.ClearDirectory(root, info => info.Name == "keep.txt");
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
        Assert.Equal("1.00KB", FileSystemUtils.FormatSize(1024));
    }

    [Fact]
    public void GetDirectorySizeBytes_DoesNotFollowReparsePoints()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "real"));
        File.WriteAllText(Path.Combine(root, "real", "a.bin"), new string('x', 4096));
        var junction = Path.Combine(root, "loop");
        try
        {
            var process = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junction}\" \"{Path.Combine(root, "real")}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
            process?.WaitForExit(5000);
            if (process?.ExitCode != 0 || !Directory.Exists(junction))
            {
                return; // 当前环境不支持创建 junction，跳过
            }

            var size = FileSystemUtils.GetDirectorySizeBytes(root);
            Assert.Equal(4096, size); // 只统计 real 一次，不跟随 loop junction
        }
        finally
        {
            // .NET 递归删除无法处理 junction 本身，先单独删 junction 再删根目录。
            if (Directory.Exists(junction))
            {
                Directory.Delete(junction);
            }

            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
