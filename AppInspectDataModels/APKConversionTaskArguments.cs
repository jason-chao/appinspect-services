using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class APKConversionTaskArguments
    {
        [JsonPropertyName("apk_filename")]
        public string APKFilename { get; set; } = string.Empty;
    }
}
