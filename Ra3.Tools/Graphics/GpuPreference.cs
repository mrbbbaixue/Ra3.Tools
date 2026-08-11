namespace Ra3.Tools.Graphics;

/// <summary>DirectX 用户 GPU 偏好：自动 / 节能 / 高性能。</summary>
public enum GpuPreference
{
    /// <summary>跟随系统设置。</summary>
    Automatic = 0,

    /// <summary>优先节能 GPU（核显）。</summary>
    Efficient = 1,

    /// <summary>优先高性能 GPU（独显）。</summary>
    Performance = 2
}
