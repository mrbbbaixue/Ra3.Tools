using Force.Crc32;

namespace Ra3.Tools.IO;

/// <summary>计算 RA3 文件 CRC32C 校验值；Fast 模式对大文件只校验头尾各 32MiB。</summary>
public static class ChecksumService
{
    private const int FastLimitBytes = 65536 * 1024;
    private const int ChunkSizeBytes = FastLimitBytes / 2;

    /// <summary>同步计算文件 CRC32C 校验值。</summary>
    /// <param name="path">文件路径。</param>
    /// <param name="mode">校验模式（Fast 只算头尾，Full 全量）。</param>
    public static uint ComputeCrc32c(string path, ChecksumMode mode)
    {
        using var stream = SharedFile.OpenRead(path);
        return ComputeCore(stream, mode);
    }

    /// <summary>异步计算文件 CRC32C 校验值（大文件读写并行双缓冲）。</summary>
    /// <param name="path">文件路径。</param>
    /// <param name="mode">校验模式（Fast 只算头尾，Full 全量）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public static async Task<uint> ComputeCrc32cAsync(string path, ChecksumMode mode, CancellationToken cancellationToken = default)
    {
        using var stream = SharedFile.OpenRead(path);
        if (stream.Length < FastLimitBytes || mode == ChecksumMode.Full)
        {
            var readBuffer = new byte[ChunkSizeBytes];
            var computeBuffer = new byte[ChunkSizeBytes];
            var checksum = 0u;
            var bytesRead = await stream.ReadAsync(readBuffer, cancellationToken);
            while (stream.Position < stream.Length)
            {
                (readBuffer, computeBuffer) = (computeBuffer, readBuffer);
                var readTask = stream.ReadAsync(readBuffer, cancellationToken);
                checksum = Crc32CAlgorithm.Append(checksum, computeBuffer, 0, bytesRead);
                bytesRead = await readTask;
            }

            return Crc32CAlgorithm.Append(checksum, readBuffer, 0, bytesRead);
        }

        var headBuffer = new byte[ChunkSizeBytes];
        var tailBuffer = new byte[ChunkSizeBytes];
        var headRead = await stream.ReadAsync(headBuffer, cancellationToken);
        stream.Seek(-tailBuffer.Length, SeekOrigin.End);
        var tailReadTask = stream.ReadAsync(tailBuffer, cancellationToken);
        var headChecksum = Crc32CAlgorithm.Compute(headBuffer, 0, headRead);
        var tailRead = await tailReadTask;
        return Crc32CAlgorithm.Append(headChecksum, tailBuffer, 0, tailRead);
    }

    private static uint ComputeCore(Stream stream, ChecksumMode mode)
    {
        if (stream.Length < FastLimitBytes || mode == ChecksumMode.Full)
        {
            var buffer = new byte[ChunkSizeBytes];
            var checksum = 0u;
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                checksum = Crc32CAlgorithm.Append(checksum, buffer, 0, bytesRead);
            }

            return checksum;
        }

        var headBuffer = new byte[ChunkSizeBytes];
        var tailBuffer = new byte[ChunkSizeBytes];
        var headRead = stream.Read(headBuffer, 0, headBuffer.Length);
        stream.Seek(-tailBuffer.Length, SeekOrigin.End);
        var tailRead = stream.Read(tailBuffer, 0, tailBuffer.Length);
        return Crc32CAlgorithm.Append(Crc32CAlgorithm.Compute(headBuffer, 0, headRead), tailBuffer, 0, tailRead);
    }
}
