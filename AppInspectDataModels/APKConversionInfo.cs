using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class APKConversionInfo
    {
        [JsonPropertyName("parquet_fullpath")]
        public string ParquetFullPath { get; set; } = string.Empty;

        [JsonPropertyName("parquet_basename")]
        public string ParquetBasename { get; set; } = string.Empty;

        [JsonPropertyName("apk_in_archive_fullfilename")]
        public string ArchivedAPKFullFilename { get; set; } = string.Empty;

        [JsonPropertyName("apk_in_archive_base_filename")]
        public string ArchivedAPKBaseFilename { get; set; } = string.Empty;

        [JsonPropertyName("basic_info")]
        public APKBasicInfo BasicInfo { get; set; } = new APKBasicInfo();

        [JsonPropertyName("signature_verified")]
        public bool SignatureVerified { get; set; } = false;

        [JsonPropertyName("signed_by_certificate")]
        public string SignedByCertificate { get; set; } = string.Empty;
    }
}
