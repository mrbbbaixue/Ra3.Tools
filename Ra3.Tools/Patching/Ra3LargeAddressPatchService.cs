using AsmResolver.IO;
using AsmResolver.PE.File;
using Ra3.Tools.IO;

namespace Ra3.Tools.Patching;

public sealed class Ra3LargeAddressPatchService
{
    public bool IsEnabled(string gameDataExecutablePath)
    {
        var bytes = SharedFile.ReadAllBytesAsync(gameDataExecutablePath).GetAwaiter().GetResult();
        var coffHeader = PEFile.FromBytes(bytes).FileHeader;
        return coffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware);
    }

    public bool Enable(string gameDataExecutablePath)
    {
        return SetState(gameDataExecutablePath, enabled: true);
    }

    public bool Disable(string gameDataExecutablePath)
    {
        return SetState(gameDataExecutablePath, enabled: false);
    }

    private static bool SetState(string gameDataExecutablePath, bool enabled)
    {
        var bytes = SharedFile.ReadAllBytesAsync(gameDataExecutablePath).GetAwaiter().GetResult();
        var coffHeader = PEFile.FromBytes(bytes).FileHeader;
        var hasFlag = coffHeader.Characteristics.HasFlag(Characteristics.LargeAddressAware);
        if (hasFlag == enabled)
        {
            return hasFlag;
        }

        var modifiedBytes = bytes.ToArray();
        using (var output = new MemoryStream(modifiedBytes, (int)coffHeader.Offset, (int)coffHeader.GetPhysicalSize()))
        {
            if (enabled)
            {
                coffHeader.Characteristics |= Characteristics.LargeAddressAware;
            }
            else
            {
                coffHeader.Characteristics &= ~Characteristics.LargeAddressAware;
            }

            coffHeader.Write(new BinaryStreamWriter(output));
        }

        var firstDifference = modifiedBytes.TakeWhile((value, index) => value == bytes[index]).Count();
        var secondPartStart = firstDifference + 1;
        if (!modifiedBytes.AsSpan(secondPartStart).SequenceEqual(bytes.AsSpan(secondPartStart)))
        {
            throw new InvalidDataException("Patched executable differs from original executable outside the expected flag byte.");
        }

        File.Move(gameDataExecutablePath,
                  gameDataExecutablePath + (enabled ? "_4gb_unpatched.backup" : "_4gb_patched.backup"),
                  overwrite: true);
        File.WriteAllBytes(gameDataExecutablePath, modifiedBytes);
        return enabled;
    }
}
