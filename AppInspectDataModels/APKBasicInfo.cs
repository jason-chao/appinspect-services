using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class APKBasicInfo
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; set; } = string.Empty;

        [JsonPropertyName("version_code")]
        public long VersionCode { get; set; } = -1;

        [JsonPropertyName("version_name")]
        public string VersionName { get; set; } = string.Empty;

        [JsonPropertyName("apk_sha256")]
        public string APK_SHA256 { get; set; } = string.Empty;
    }
}
