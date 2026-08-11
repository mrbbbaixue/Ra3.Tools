namespace Ra3.Tools.Configuration;

/// <summary>SkuDef 文档中一行条目的抽象基类。</summary>
public abstract record SkudefEntry;

/// <summary>"set-search-path" 条目。</summary>
/// <param name="Command">搜索路径参数。</param>
public sealed record SetSearchPathEntry(string Command) : SkudefEntry;

/// <summary>"add-config" 条目。</summary>
/// <param name="Path">配置（.txt/.ini）文件路径。</param>
public sealed record AddConfigEntry(string Path) : SkudefEntry;

/// <summary>"add-big" 条目。</summary>
/// <param name="Path">BIG 包文件路径。</param>
public sealed record AddBigEntry(string Path) : SkudefEntry;

/// <summary>无法识别的原始行（原样保留，写回时逐字输出）。</summary>
/// <param name="Text">原始行文本。</param>
public sealed record RawEntry(string Text) : SkudefEntry;
