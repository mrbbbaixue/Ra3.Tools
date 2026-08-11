using Ra3.Tools.Configuration;

namespace Ra3.Tools.Test.Configuration;

public sealed class SkudefSerializerTests
{
    [Fact]
    public void LoadAndSave_PreservesRecognizedAndRawEntries()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.skudef");
        File.WriteAllLines(tempPath,
        [
            "mod-game 1.12",
            "set-search-path big:;.;D:\\Games\\RA3",
            "add-config D:\\Configs\\base.skudef",
            "custom-command something",
            "add-big D:\\Mods\\example.big"
        ]);

        try
        {
            var document = SkudefSerializer.Load(tempPath);
            SkudefSerializer.Save(tempPath, document);

            var savedLines = File.ReadAllLines(tempPath);
            Assert.Equal("mod-game 1.12", savedLines[0]);
            Assert.Contains("set-search-path big:;.;D:\\Games\\RA3", savedLines);
            Assert.Contains("add-config D:\\Configs\\base.skudef", savedLines);
            Assert.Contains("custom-command something", savedLines);
            Assert.Contains("add-big D:\\Mods\\example.big", savedLines);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
