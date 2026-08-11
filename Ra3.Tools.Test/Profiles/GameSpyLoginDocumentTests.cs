using Ra3.Tools.Profiles;

namespace Ra3.Tools.Test.Profiles;

public sealed class GameSpyLoginDocumentTests
{
    // 合成测试向量（非真实用户数据）：加密格式与 RA3 引擎 GameSpyLoginPreferences 一致。
    private const string EncryptedSamplePassword = "B3PE_28_01_3E_0A_16_16_10";
    private const string SamplePassword = "s3cretPass!";

    [Fact]
    public void DecryptPassword_DecryptsSyntheticSample()
    {
        var account = new GameSpyAccount("player1", null, EncryptedSamplePassword, null);

        Assert.Equal(SamplePassword, account.DecryptPassword());
    }

    [Fact]
    public void EncryptAndDecrypt_AreInverseOperations()
    {
        // 编解码为单字节域（引擎 AsciiString 语义），密码为 ASCII 可打印字符。
        foreach (var password in new[] { SamplePassword, "abc123", "p@ss w0rd_!", "MixedCase99" })
        {
            var encrypted = GameSpyPasswordCodec.Encrypt(password);
            Assert.Equal(password, GameSpyPasswordCodec.Decrypt(encrypted));
        }
    }

    [Fact]
    public void Load_ParsesRememberedAccounts()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ini");
        File.WriteAllText(path,
            "lastEmail = player1\r\n" +
            "lastProfileId = 100021\r\n" +
            "OneTimeGamerCount = 1\r\n" +
            "useProfiles = yes\r\n" +
            $"pass_player1 = {EncryptedSamplePassword}\r\n" +
            "date_player1 = 02_2F01_2F2024\r\n" +
            "nick_player1 = TestNick1,TestNick2\r\n");

        try
        {
            var document = GameSpyLoginDocument.Load(path);

            Assert.Equal("player1", document.LastEmail);
            Assert.Equal(100021, document.LastProfileId);
            Assert.Equal(1, document.OneTimeGamerCount);
            Assert.True(document.UseProfiles);
            var account = Assert.Single(document.Accounts);
            Assert.Equal("player1", account.Email);
            Assert.Equal("TestNick1,TestNick2", account.Nickname);
            Assert.Equal(SamplePassword, account.DecryptPassword());
            Assert.Equal(new DateTime(2024, 2, 1), account.LoginDate);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyDocument()
    {
        var document = GameSpyLoginDocument.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ini"));

        Assert.Empty(document.Accounts);
        Assert.Equal(string.Empty, document.LastEmail);
        Assert.Equal(0, document.LastProfileId);
        Assert.False(document.UseProfiles);
    }

    [Fact]
    public void Load_WithoutAccountKeys_ReturnsNoAccounts()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ini");
        File.WriteAllText(path, "lastEmail = \r\nlastProfileId = 0\r\n");

        try
        {
            var document = GameSpyLoginDocument.Load(path);
            Assert.Empty(document.Accounts);
            Assert.Equal(0, document.LastProfileId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_MalformedDate_ReturnsNullLoginDate()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ini");
        File.WriteAllText(path, $"pass_user = {EncryptedSamplePassword}\r\ndate_user = not_a_date\r\n");

        try
        {
            var document = GameSpyLoginDocument.Load(path);
            var account = Assert.Single(document.Accounts);
            Assert.Null(account.LoginDate);
            Assert.Equal(SamplePassword, account.DecryptPassword());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
