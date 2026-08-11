using AsmResolver.PE.File;
using Ra3.Tools.IO;

namespace Ra3.Tools.Patching;

/// <summary>
/// 为 ra3_1.12.game 启用/关闭 4GB Patch（LARGEADDRESSAWARE 标志）。
/// 只读写 PE COFF 头区域，不将整个游戏文件载入内存；修改前生成备份并校验除标志位外无其他差异。
/// </summary>
public sealed class Ra3LargeAddressPatcher
{
    // COFF 头内 Characteristics 位于偏移 18（Machine 2 + NumberOfSections 2 + TimeDateStamp 4 + PointerToSymbolTable 4 + NumberOfSymbols 4 + SizeOfOptionalHeader 2）。
    private const int CharacteristicsOffsetInCoffHeader = 18;

    /// <summary>检查游戏数据文件当前是否已启用 LARGEADDRESSAWARE。</summary>
    /// <param name="gameDataExecutablePath">ra3_1.12.game 路径。</param>
    /// <exception cref="System.BadImageFormatException">文件不是有效的 PE 文件时抛出。</exception>
    public bool IsEnabled(string gameDataExecutablePath)
    {
        using var stream = SharedFile.OpenRead(gameDataExecutablePath);
        return ReadFileHeader(stream).Characteristics.HasFlag(Characteristics.LargeAddressAware);
    }

    /// <summary>启用 4GB Patch，并生成 "&lt;文件&gt;_4gb_unpatched.backup" 备份；已启用时返回 true 且不修改文件。</summary>
    /// <param name="gameDataExecutablePath">ra3_1.12.game 路径。</param>
    public bool Enable(string gameDataExecutablePath)
    {
        return SetState(gameDataExecutablePath, enabled: true);
    }

    /// <summary>关闭 4GB Patch，并生成 "&lt;文件&gt;_4gb_patched.backup" 备份；已关闭时返回 false 且不修改文件。</summary>
    /// <param name="gameDataExecutablePath">ra3_1.12.game 路径。</param>
    public bool Disable(string gameDataExecutablePath)
    {
        return SetState(gameDataExecutablePath, enabled: false);
    }

    private static bool SetState(string gameDataExecutablePath, bool enabled)
    {
        using var stream = SharedFile.OpenReadWrite(gameDataExecutablePath);
        var fileHeader = ReadFileHeader(stream);
        var hasFlag = fileHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware);
        if (hasFlag == enabled)
        {
            return hasFlag;
        }

        var original = ReadBytes(stream, checked((long)fileHeader.Offset), checked((int)fileHeader.GetPhysicalSize()));
        var modified = (byte[])original.Clone();
        var characteristics = (ushort)fileHeader.Characteristics;
        if (enabled)
        {
            characteristics |= (ushort)Characteristics.LargeAddressAware;
        }
        else
        {
            characteristics &= (ushort)~Characteristics.LargeAddressAware;
        }

        Buffer.BlockCopy(BitConverter.GetBytes(characteristics), 0, modified, CharacteristicsOffsetInCoffHeader, sizeof(ushort));

        // 防御校验：COFF 头除 Characteristics 两字节外必须无差异，防止误改加密/损坏文件。
        for (var index = 0; index < original.Length; index++)
        {
            if (index is >= CharacteristicsOffsetInCoffHeader and < CharacteristicsOffsetInCoffHeader + sizeof(ushort))
            {
                continue;
            }

            if (original[index] != modified[index])
            {
                throw new InvalidDataException("Patched executable differs from original executable outside the expected flag bytes.");
            }
        }

        // 使用 Copy 而非 Move：文件句柄已打开，Move 会把打开的句柄重定向到备份文件。
        File.Copy(gameDataExecutablePath,
                  gameDataExecutablePath + (enabled ? "_4gb_unpatched.backup" : "_4gb_patched.backup"),
                  overwrite: true);
        stream.Seek(checked((long)fileHeader.Offset), SeekOrigin.Begin);
        stream.Write(modified);
        return enabled;
    }

    private static FileHeader ReadFileHeader(Stream stream)
    {
        return PEFile.FromStream(stream).FileHeader;
    }

    private static byte[] ReadBytes(Stream stream, long offset, int count)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        var buffer = new byte[count];
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = stream.Read(buffer, totalRead, count - totalRead);
            if (read == 0)
            {
                throw new InvalidDataException("Unexpected end of file while reading the COFF header.");
            }

            totalRead += read;
        }

        return buffer;
    }
}
