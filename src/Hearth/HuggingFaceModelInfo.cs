using System.Text.Json.Serialization;

namespace Hearth;

internal sealed class HuggingFaceModelInfo
{
    [JsonPropertyName("siblings")]
    public List<HuggingFaceFile> Siblings { get; set; } = [];
}

internal sealed class HuggingFaceFile
{
    [JsonPropertyName("rfilename")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("lfs")]
    public HuggingFaceLfs? Lfs { get; set; }
}

internal sealed class HuggingFaceLfs
{
    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
