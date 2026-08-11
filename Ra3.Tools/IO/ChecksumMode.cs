namespace Ra3.Tools.IO;

/// <summary>CRC32C 校验模式：Fast 对大文件只校验头尾各 32MiB，Full 全量计算。</summary>
public enum ChecksumMode
{
    /// <summary>快速模式（大文件只算头尾）。</summary>
    Fast,

    /// <summary>全量模式。</summary>
    Full
}
