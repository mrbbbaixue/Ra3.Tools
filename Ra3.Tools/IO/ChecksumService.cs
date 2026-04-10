using Force.Crc32;

namespace Ra3.Tools.IO;

public static class ChecksumService
{
    public static async Task<uint> ComputeCrc32cAsync(string path, ChecksumMode mode, CancellationToken cancellationToken = default)
    {
        using var stream = SharedFile.OpenRead(path);
        const int fastLimit = 65536 * 1024;
        const int chunkSize = fastLimit / 2;

        if (stream.Length < fastLimit || mode == ChecksumMode.Full)
        {
            var readBuffer = new byte[chunkSize];
            var computeBuffer = new byte[chunkSize];
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

        var headBuffer = new byte[chunkSize];
        var tailBuffer = new byte[chunkSize];
        var headRead = await stream.ReadAsync(headBuffer, cancellationToken);
        stream.Seek(-tailBuffer.Length, SeekOrigin.End);
        var tailReadTask = stream.ReadAsync(tailBuffer, cancellationToken);
        var headChecksum = Crc32CAlgorithm.Compute(headBuffer, 0, headRead);
        var tailRead = await tailReadTask;
        return Crc32CAlgorithm.Append(headChecksum, tailBuffer, 0, tailRead);
    }
}
