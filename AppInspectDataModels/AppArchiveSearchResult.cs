using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class AppArchiveSearchResult
    {
        public string AppId { get; set; }
        public string Title { get; set; }
        public int ApkFileCount { get; set; }
        public int StoreQueryCount { get; set; }
        public string? LatestVersionName { get; set; }
    }
}
