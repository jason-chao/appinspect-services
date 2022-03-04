namespace AppInspectServices.Models
{
    public class DatabaseSettings
    {
        public string AppEntryCollectionName { get; set; } = string.Empty;
        public string AppFileRecordCollectionName { get; set; } = string.Empty;
        public string AppStoreRecordCollectionName { get; set; } = string.Empty;
        public string TaskCollectionName { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public int PreventRepeatedAPKRetrievalInHours { get; set; } = 0;
    }
}
