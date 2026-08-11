namespace Ra3.Tools.IO;

/// <summary>
/// 以最大共享模式（ReadWrite | Delete）打开 RA3 相关文件，解决游戏进程占用文件时的读写问题。
/// </summary>
public static class SharedFile
{
    /// <summary>以读共享方式打开文件（允许他人读写和删除）。</summary>
    /// <param name="path">文件路径。</param>
    public static FileStream OpenRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }

    /// <summary>以读写共享方式打开文件（允许他人读写和删除）。</summary>
    /// <param name="path">文件路径。</param>
    public static FileStream OpenReadWrite(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
    }

    /// <summary>以共享读模式打开文本读取器。</summary>
    /// <param name="path">文件路径。</param>
    public static StreamReader OpenText(string path)
    {
        return new StreamReader(OpenRead(path));
    }

    /// <summary>以共享读模式读取文件全部字节。</summary>
    /// <param name="path">文件路径。</param>
    public static async Task<byte[]> ReadAllBytesAsync(string path)
    {
        using var input = OpenRead(path);
        using var output = new MemoryStream();
        await input.CopyToAsync(output);
        return output.ToArray();
    }
}
