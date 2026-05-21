namespace Hearth.Vulkan;

/// <summary>
/// Marker type that activates the Vulkan native backend for Hearth.
/// Install this package and set <c>GpuLayers &gt; 0</c> in <see cref="HearthOptions"/> to offload
/// transformer layers to your AMD, Intel, or other Vulkan-capable GPU.
/// </summary>
public static class HearthVulkanBackend { }
