using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class ApkAnalysisWorkerResults
    {
        [JsonPropertyName("total")]
        public int Total { get; set; } = -1;
        [JsonPropertyName("output_limit")]
        public int OutputLimit { get; set; } = int.MaxValue;
        [JsonPropertyName("records")]
        public List<ApkAnalysisRecord> Records { get; set; } = new List<ApkAnalysisRecord>();
    }
}
