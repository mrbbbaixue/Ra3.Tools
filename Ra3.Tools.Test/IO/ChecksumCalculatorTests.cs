using Force.Crc32;
using Ra3.Tools.IO;

namespace Ra3.Tools.Test.IO;

public sealed class ChecksumCalculatorTests
{
    [Fact]
    public async Task ComputeCrc32cAsync_ReturnsSameResultForSmallFile()
    {
        var tempPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempPath, "hello world");

        try
        {
            var fast = await ChecksumCalculator.ComputeCrc32cAsync(tempPath, ChecksumMode.Fast);
            var full = await ChecksumCalculator.ComputeCrc32cAsync(tempPath, ChecksumMode.Full);
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
            var fast = await ChecksumCalculator.ComputeCrc32cAsync(tempPath, ChecksumMode.Fast);
            var full = await ChecksumCalculator.ComputeCrc32cAsync(tempPath, ChecksumMode.Full);
            Assert.NotEqual(full, fast);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    /// <summary>
    /// 钉住两种模式各自覆盖哪些字节：Fast 只算头尾各 32 MiB，Full 算整份。
    /// 读盘缓冲改成分段读之后，这条用例负责证明「Fast 的含义没跟着变」。
    /// </summary>
    [Fact]
    public async Task ComputeCrc32c_ModesCoverExpectedRanges()
    {
        const int fastWindow = 32 * 1024 * 1024;
        const int length = 70 * 1024 * 1024; // 超过 64 MiB 阈值：Fast 只取头尾，Full 取全份
        var tempPath = Path.GetTempFileName();
        try
        {
            var chunk = new byte[1024 * 1024];
            uint wholeFile = 0;
            await using (var file = File.Create(tempPath))
            {
                for (var i = 0; i < length / chunk.Length; i++)
                {
                    Array.Fill(chunk, (byte)(i * 37));
                    await file.WriteAsync(chunk);
                    wholeFile = Crc32CAlgorithm.Append(wholeFile, chunk, 0, chunk.Length);
                }
            }

            uint headAndTail;
            await using (var stream = File.OpenRead(tempPath))
            {
                var window = new byte[fastWindow];
                var head = await stream.ReadAsync(window);
                headAndTail = Crc32CAlgorithm.Append(0, window, 0, head);
                stream.Seek(-fastWindow, SeekOrigin.End);
                var tail = await stream.ReadAsync(window);
                headAndTail = Crc32CAlgorithm.Append(headAndTail, window, 0, tail);
            }

            Assert.Equal(wholeFile, await ChecksumCalculator.ComputeCrc32cAsync(tempPath, ChecksumMode.Full));
            Assert.Equal(wholeFile, ChecksumCalculator.ComputeCrc32c(tempPath, ChecksumMode.Full));
            Assert.Equal(headAndTail, await ChecksumCalculator.ComputeCrc32cAsync(tempPath, ChecksumMode.Fast));
            Assert.Equal(headAndTail, ChecksumCalculator.ComputeCrc32c(tempPath, ChecksumMode.Fast));
            Assert.NotEqual(wholeFile, headAndTail);
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
            var sync = ChecksumCalculator.ComputeCrc32c(tempPath, ChecksumMode.Full);
            var async = await ChecksumCalculator.ComputeCrc32cAsync(tempPath, ChecksumMode.Full);
            Assert.Equal(async, sync);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
