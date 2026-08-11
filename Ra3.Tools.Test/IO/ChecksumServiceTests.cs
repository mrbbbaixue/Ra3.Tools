using Ra3.Tools.IO;

namespace Ra3.Tools.Test.IO;

public sealed class ChecksumServiceTests
{
    [Fact]
    public async Task ComputeCrc32cAsync_ReturnsSameResultForSmallFile()
    {
        var tempPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempPath, "hello world");

        try
        {
            var fast = await ChecksumService.ComputeCrc32cAsync(tempPath, ChecksumMode.Fast);
            var full = await ChecksumService.ComputeCrc32cAsync(tempPath, ChecksumMode.Full);
            Assert.Equal(full, fast);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ComputeCrc32cAsync_ReturnsDifferentFastAndFullChecksumsForLargeFile()
    {
        var tempPath = Path.GetTempFileName();
        var fileLength = 70 * 1024 * 1024;
        var bytes = new byte[fileLength];
        Array.Fill(bytes, (byte)0x00);
        Array.Fill(bytes, (byte)0xFF, 1024 * 1024, 2 * 1024 * 1024);
        await File.WriteAllBytesAsync(tempPath, bytes);

        try
        {
            var fast = await ChecksumService.ComputeCrc32cAsync(tempPath, ChecksumMode.Fast);
            var full = await ChecksumService.ComputeCrc32cAsync(tempPath, ChecksumMode.Full);
            Assert.NotEqual(full, fast);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ComputeCrc32c_SyncOverload_MatchesAsyncResult()
    {
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "hello world");

        try
        {
            var sync = ChecksumService.ComputeCrc32c(tempPath, ChecksumMode.Full);
            var async = await ChecksumService.ComputeCrc32cAsync(tempPath, ChecksumMode.Full);
            Assert.Equal(async, sync);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
