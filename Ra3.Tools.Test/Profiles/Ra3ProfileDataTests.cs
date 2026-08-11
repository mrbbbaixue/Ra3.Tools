using Ra3.Tools.Profiles;

namespace Ra3.Tools.Test.Profiles;

public sealed class Ra3ProfileDataTests
{
    // 合成测试数据（非真实用户 PPK）。
    private const string SampleKeyWithVersion = "12345678901234567:1";
    private const string SampleKeyWithoutVersion = "223456789012345678";

    [Fact]
    public void Parse_WithVersion_ExtractsKeyAndVersion()
    {
        var data = Ra3ProfileData.Parse(SampleKeyWithVersion);

        Assert.Equal(12345678901234567, data.PublicKey);
        Assert.Equal(1, data.Version);
    }

    [Fact]
    public void Parse_WithoutVersion_ExtractsKeyOnly()
    {
        var data = Ra3ProfileData.Parse(SampleKeyWithoutVersion);

        Assert.Equal(223456789012345678, data.PublicKey);
        Assert.Null(data.Version);
    }

    [Fact]
    public void Parse_InvalidValue_ReturnsEmpty()
    {
        Assert.Equal(new Ra3ProfileData(null, null), Ra3ProfileData.Parse("abc"));
        Assert.Equal(new Ra3ProfileData(null, null), Ra3ProfileData.Parse(string.Empty));
    }

    [Fact]
    public void Parse_InvalidVersion_KeepsKeyAndReturnsNullVersion()
    {
        var data = Ra3ProfileData.Parse("123:not-a-number");

        Assert.Equal(123, data.PublicKey);
        Assert.Null(data.Version);
    }

    [Fact]
    public void Load_ReadsPpkFromFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ini");
        File.WriteAllText(path, $"PPK = {SampleKeyWithVersion}\r\n");

        try
        {
            var data = Ra3ProfileData.Load(path);
            Assert.Equal(12345678901234567, data.PublicKey);
            Assert.Equal(1, data.Version);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmpty()
    {
        var data = Ra3ProfileData.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ini"));

        Assert.Equal(new Ra3ProfileData(null, null), data);
    }
}
