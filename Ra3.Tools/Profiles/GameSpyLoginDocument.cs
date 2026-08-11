using System.Globalization;

namespace Ra3.Tools.Profiles;

/// <summary>
/// 解析 RA3 档案目录下的 GameSpyLogin.ini（在线服务登录信息）。
/// 包含记住的账号（pass_/nick_/date_ 键），密码为引擎混淆格式，可用 <see cref="GameSpyAccount.DecryptPassword"/> 还原。
/// </summary>
public sealed class GameSpyLoginDocument
{
    private const string LastEmailKey = "lastEmail";
    private const string LastProfileIdKey = "lastProfileId";
    private const string OneTimeGamerCountKey = "OneTimeGamerCount";
    private const string UseProfilesKey = "useProfiles";

    private GameSpyLoginDocument(string lastEmail,
                                 int lastProfileId,
                                 int oneTimeGamerCount,
                                 bool useProfiles,
                                 IReadOnlyList<GameSpyAccount> accounts)
    {
        LastEmail = lastEmail;
        LastProfileId = lastProfileId;
        OneTimeGamerCount = oneTimeGamerCount;
        UseProfiles = useProfiles;
        Accounts = accounts;
    }

    /// <summary>最近一次登录使用的邮箱/账号。</summary>
    public string LastEmail { get; }

    /// <summary>最近一次登录的 GameSpy 档案 ID；未设置时为 0。</summary>
    public int LastProfileId { get; }

    /// <summary>曾记住的账号数量。</summary>
    public int OneTimeGamerCount { get; }

    /// <summary>是否使用档案登录（useProfiles = yes）。</summary>
    public bool UseProfiles { get; }

    /// <summary>记住的在线账号列表（按文件中的出现顺序）。</summary>
    public IReadOnlyList<GameSpyAccount> Accounts { get; }

    /// <summary>从文件加载登录信息；文件不存在时返回空文档（LastProfileId = 0、无账号）。</summary>
    /// <param name="path">GameSpyLogin.ini 路径。</param>
    public static GameSpyLoginDocument Load(string path)
    {
        if (!File.Exists(path))
        {
            return new GameSpyLoginDocument(string.Empty, 0, 0, false, Array.Empty<GameSpyAccount>());
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] is ';' or '#')
            {
                continue;
            }

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
            {
                continue;
            }

            var key = line[..equalsIndex].Trim();
            if (key.Length > 0)
            {
                values[key] = line[(equalsIndex + 1)..].Trim();
            }
        }

        var accounts = BuildAccounts(values);
        return new GameSpyLoginDocument(
            values.GetValueOrDefault(LastEmailKey, string.Empty),
            TryParseInt(values.GetValueOrDefault(LastProfileIdKey)),
            TryParseInt(values.GetValueOrDefault(OneTimeGamerCountKey)),
            values.GetValueOrDefault(UseProfilesKey)?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true,
            accounts);
    }

    private static IReadOnlyList<GameSpyAccount> BuildAccounts(IReadOnlyDictionary<string, string> values)
    {
        var emails = new List<string>();
        foreach (var key in values.Keys)
        {
            if (TryGetAccountKey(key, out var email) && !emails.Contains(email, StringComparer.OrdinalIgnoreCase))
            {
                emails.Add(email);
            }
        }

        return emails
            .Select(email => new GameSpyAccount(
                email,
                values.GetValueOrDefault($"nick_{email}"),
                values.GetValueOrDefault($"pass_{email}"),
                TryParseDate(values.GetValueOrDefault($"date_{email}"))))
            .ToArray();
    }

    private static bool TryGetAccountKey(string key, out string email)
    {
        email = string.Empty;
        foreach (var prefix in new[] { "pass_", "nick_", "date_" })
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && key.Length > prefix.Length)
            {
                email = key[prefix.Length..];
                return true;
            }
        }

        return false;
    }

    private static int TryParseInt(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
    }

    private static DateTime? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // date_ 值同样是 QP 变体编码（如 "02_2F01_2F2024" 表示 "02/01/2024"），先解码再解析。
        var decoded = GameSpyPasswordCodec.DecodeDate(value);
        if (DateTime.TryParseExact(decoded, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}

/// <summary>GameSpyLogin.ini 中记住的一个在线账号。</summary>
/// <param name="Email">账号（pass_/nick_/date_ 键的后缀）。</param>
/// <param name="Nickname">在线昵称（UTF-8，可为 null）。</param>
/// <param name="EncryptedPassword">引擎混淆格式的密码原文（可为 null，表示未保存密码）。</param>
/// <param name="LoginDate">最后登录日期（可为 null）。</param>
public sealed record GameSpyAccount(string Email, string? Nickname, string? EncryptedPassword, DateTime? LoginDate)
{
    /// <summary>解密保存的密码（Quoted-Printable 变体解码 + XOR 混淆还原）；未保存密码返回 null。</summary>
    public string? DecryptPassword()
    {
        return EncryptedPassword is null ? null : GameSpyPasswordCodec.Decrypt(EncryptedPassword);
    }
}
