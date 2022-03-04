using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class APKFileInfo
    {
        [JsonPropertyName("base_filename")]
        public string BaseFilename { get; set; } = string.Empty;

        [JsonPropertyName("full_filename")]
        public string FullFilename { get; set; } = string.Empty;

        [JsonPropertyName("file_sha256")]
        public string SHA256 { get; set; } = string.Empty;
    }
}
