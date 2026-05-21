namespace Hearth.Metal;

/// <summary>
/// Marker type that activates the Apple Metal native backend for Hearth.
/// Install this package and set <c>GpuLayers = 999</c> in <see cref="HearthOptions"/> to offload
/// all transformer layers to Apple Silicon's Neural Engine / GPU.
/// </summary>
public static class HearthMetalBackend { }
