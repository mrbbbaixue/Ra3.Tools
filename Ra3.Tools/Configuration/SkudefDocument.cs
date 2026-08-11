namespace Ra3.Tools.Configuration;

/// <summary>一个 SkuDef 文档：mod-game 头行 + 有序条目列表。</summary>
/// <param name="Header">mod-game 头行（如 "mod-game 1.12"）。</param>
/// <param name="Entries">有序条目列表，未知行以 <see cref="RawEntry"/> 原样保留。</param>
public sealed record SkudefDocument(string Header, IReadOnlyList<SkudefEntry> Entries);
