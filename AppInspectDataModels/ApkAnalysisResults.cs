namespace AppInspectDataModels
{
    public class ApkAnalysisResults
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string ArgumentsPrettifed { get; set; } = string.Empty;
        public int OutputApks { get; set; } = -1;
        public int OutputApps { get; set; } = -1;
        public int OutputApkLimit { get; set; } = int.MaxValue;
        public int? TotalInferredDevelopers { get; set; }
        public List<string>? InferredDevelopers { get; set; }
        public int? TotalTrackers { get; set; }
        public List<string>? Trackers { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? Started { get; set; }
        public DateTime? Completed { get; set; }
        public List<GrouppedApkAnalysisRecord> Records { get; set; } = new List<GrouppedApkAnalysisRecord>();
    }
}
