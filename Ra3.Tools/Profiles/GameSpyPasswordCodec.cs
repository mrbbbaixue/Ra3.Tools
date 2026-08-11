namespace Ra3.Tools.Profiles;

/// <summary>
/// RA3 GameSpy 在线账号密码编解码器。
/// 算法与 RA3 引擎的 GameSpyLoginPreferences 一致：Quoted-Printable 变体编码
/// （非字母数字字节写为 "_XX"）+ XOR 混淆密钥 "1337Munkee"（循环，与密钥相同的字节原样保留）。
/// </summary>
internal static class GameSpyPasswordCodec
{
    private const string ObfuscationKey = "1337Munkee";
    private const char MagicChar = '_';

    /// <summary>解密：QP 变体解码后逐字节 XOR 密钥。</summary>
    internal static string Decrypt(string encryptedValue)
    {
        var bytes = Xor(Decode(encryptedValue));
        return System.Text.Encoding.Latin1.GetString(bytes);
    }

    /// <summary>QP 变体解码为 ASCII 字符串（用于 date_ 等纯 ASCII 值，如 "02_2F01_2F2024" → "02/01/2024"）。</summary>
    internal static string DecodeDate(string value)
    {
        return System.Text.Encoding.ASCII.GetString(Decode(value));
    }

    /// <summary>加密：逐字节 XOR 密钥后做 QP 变体编码（与解密互逆）。</summary>
    internal static string Encrypt(string plainText)
    {
        var bytes = Xor(System.Text.Encoding.Latin1.GetBytes(plainText));
        return Encode(bytes);
    }

    private static byte[] Xor(byte[] data)
    {
        var result = new byte[data.Length];
        for (var index = 0; index < data.Length; index++)
        {
            var key = (byte)ObfuscationKey[index % ObfuscationKey.Length];
            result[index] = data[index] == key ? data[index] : (byte)(data[index] ^ key);
        }

        return result;
    }

    private static byte[] Decode(string value)
    {
        var result = new List<byte>(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == MagicChar && index + 2 < value.Length &&
                byte.TryParse(value.AsSpan(index + 1, 2), System.Globalization.NumberStyles.HexNumber, null, out var decoded))
            {
                result.Add(decoded);
                index += 2;
                continue;
            }

            result.Add((byte)value[index]);
        }

        return result.ToArray();
    }

    private static string Encode(byte[] data)
    {
        var builder = new System.Text.StringBuilder(data.Length);
        foreach (var current in data)
        {
            if (IsAlphaNumeric(current))
            {
                builder.Append((char)current);
                continue;
            }

            builder.Append(MagicChar)
                .Append(current.ToString("X2"));
        }

        return builder.ToString();
    }

    private static bool IsAlphaNumeric(byte value)
    {
        return value is >= (byte)'0' and <= (byte)'9'
            or >= (byte)'A' and <= (byte)'Z'
            or >= (byte)'a' and <= (byte)'z';
    }
}
