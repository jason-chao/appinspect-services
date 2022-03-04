using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class AppStoreQueryTaskArguments
    {
        [JsonPropertyName("query_method")]
        public string QueryMethod { get; set; } = string.Empty;
        [JsonPropertyName("query_json_string")]
        public string QueryInJson { get; set; } = string.Empty;

        [JsonPropertyName("automatic_apk_retrieval")]
        public bool AutomaticAPKRetrieval { get; set; } = false;
    }
}
