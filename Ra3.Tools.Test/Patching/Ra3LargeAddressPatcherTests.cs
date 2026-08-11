using Ra3.Tools.Patching;

namespace Ra3.Tools.Test.Patching;

public sealed class Ra3LargeAddressPatcherTests
{
    [Fact]
    public void EnableAndDisable_ToggleLargeAddressAwareAndCreateBackups()
    {
        var sourceAssembly = typeof(Ra3LargeAddressPatcher).Assembly.Location;
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var tempPath = Path.Combine(tempDirectory, "ra3_1.12.game");
        File.Copy(sourceAssembly, tempPath, overwrite: true);

        var service = new Ra3LargeAddressPatcher();

        try
        {
            if (service.IsEnabled(tempPath))
            {
                service.Disable(tempPath);
            }

            Assert.False(service.IsEnabled(tempPath));
            Assert.True(service.Enable(tempPath));
            Assert.True(service.IsEnabled(tempPath));
            Assert.True(File.Exists(tempPath + "_4gb_unpatched.backup"));

            Assert.False(service.Disable(tempPath));
            Assert.False(service.IsEnabled(tempPath));
            Assert.True(File.Exists(tempPath + "_4gb_patched.backup"));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
