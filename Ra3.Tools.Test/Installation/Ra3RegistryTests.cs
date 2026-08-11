using Microsoft.Win32;
using Ra3.Tools.Installation;
using Ra3.Tools.Test.TestDoubles;

namespace Ra3.Tools.Test.Installation;

public sealed class Ra3RegistryTests
{
    [Fact]
    public void GetStatus_ReturnsMissingGameKeyWhenRegistryIsEmpty()
    {
        var registry = new Ra3Registry(new MemoryRegistryBackend());

        Assert.Equal(RegistryStatus.MissingGameKey, registry.GetStatus());
    }

    [Fact]
    public void Rebuild_PopulatesKeysAndClear_RemovesThem()
    {
        var backend = new MemoryRegistryBackend();
        var registry = new Ra3Registry(backend);
        var installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true);

        try
        {
            registry.Rebuild(installPath, "TEST-KEY");

            Assert.Equal(RegistryStatus.Correct, registry.GetStatus());
            Assert.Equal(Path.GetFullPath(installPath), registry.GetInstallPath());
            Assert.Equal("TEST-KEY",
                         backend.GetValue(RegistryHive.LocalMachine,
                                          RegistryView.Registry32,
                                          @"Software\Electronic Arts\Electronic Arts\Red Alert 3\ergc",
                                          null));

            registry.Clear();

            Assert.Equal(RegistryStatus.MissingGameKey, registry.GetStatus());
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }

    [Fact]
    public void TrySetLanguage_UpdatesBothRegistryHives()
    {
        var backend = new MemoryRegistryBackend();
        var registry = new Ra3Registry(backend);
        var installPath = Ra3InstallationValidatorTests.CreateInstallDirectory(includeLauncher: true);
        File.WriteAllText(Path.Combine(installPath, "Launcher", "chinese_s.csf"), string.Empty);
        File.WriteAllText(Path.Combine(installPath, "RA3_chinese_s_1.12.SkuDef"), "mod-game 1.12");

        try
        {
            registry.Rebuild(installPath, "TEST-KEY");

            Assert.True(registry.TrySetLanguage(installPath, "chinese_s"));
            Assert.Equal("Chinese (Simplified)",
                         backend.GetValue(RegistryHive.LocalMachine,
                                          RegistryView.Registry32,
                                          @"Software\Electronic Arts\Electronic Arts\Red Alert 3",
                                          "language"));
            Assert.Equal("chinese_s",
                         backend.GetValue(RegistryHive.CurrentUser,
                                          RegistryView.Default,
                                          @"Software\Electronic Arts\Electronic Arts\Red Alert 3",
                                          "Language"));
        }
        finally
        {
            Directory.Delete(installPath, recursive: true);
        }
    }
}
