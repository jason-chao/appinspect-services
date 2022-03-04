using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class AppArchiveSearch
    {
        [JsonPropertyName("appIds")]
        public List<string>? AppIds { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }
}
