using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AppInspectDataModels
{
    public class AppEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class AppEntryAllRecords: AppEntry
    {
        public List<AppFileRecord> FileRecords { get; set; } = new List<AppFileRecord>();
        public List<AppStoreRecordResult> AppStoreRecords { get; set; } = new List<AppStoreRecordResult>();
    }
}
