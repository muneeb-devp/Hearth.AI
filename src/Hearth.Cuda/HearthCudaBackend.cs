namespace Hearth.Cuda;

/// <summary>
/// Marker type that activates the NVIDIA CUDA 12 native backend for Hearth.
/// Install this package and set <c>GpuLayers &gt; 0</c> in <see cref="HearthOptions"/> to offload
/// transformer layers to your CUDA-capable GPU.
/// </summary>
public static class HearthCudaBackend { }
