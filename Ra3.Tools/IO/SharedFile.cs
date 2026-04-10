namespace Ra3.Tools.IO;

public static class SharedFile
{
    public static FileStream OpenRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }

    public static StreamReader OpenText(string path)
    {
        return new StreamReader(OpenRead(path));
    }

    public static async Task<byte[]> ReadAllBytesAsync(string path)
    {
        using var input = OpenRead(path);
        using var output = new MemoryStream();
        await input.CopyToAsync(output);
        return output.ToArray();
    }
}
