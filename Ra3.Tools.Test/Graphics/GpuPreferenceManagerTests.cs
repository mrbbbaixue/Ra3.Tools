using Microsoft.Win32;
using Ra3.Tools.Graphics;
using Ra3.Tools.Test.TestDoubles;

namespace Ra3.Tools.Test.Graphics;

public sealed class GpuPreferenceManagerTests
{
    [Fact]
    public void SetAndGet_RoundTripPreferenceThroughRegistryBackend()
    {
        var backend = new MemoryRegistryBackend();
        var manager = new GpuPreferenceManager(backend);
        var executablePath = @"D:\Games\RA3\Data\ra3_1.12.game";

        manager.Set(executablePath, GpuPreference.Performance);

        Assert.Equal(GpuPreference.Performance, manager.Get(executablePath));
        Assert.Equal("GpuPreference=2;",
                     backend.GetValue(RegistryHive.CurrentUser,
                                      RegistryView.Default,
                                      @"Software\Microsoft\DirectX\UserGpuPreferences",
                                      Path.GetFullPath(executablePath)));
    }
}
