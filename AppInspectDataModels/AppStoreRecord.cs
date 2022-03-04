using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class AppStoreRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = string.Empty;

        public string AppId { get; set; } = string.Empty;
        public DateTime Retrieved { get; set; }
        public string? JsonSHA256 { get; set; }
        public string? StoreName { get; set; }

        public string? StoreCountry { get; set; }
        public string? StoreLanguage { get; set; }

        //public Locale? Locale { get; set; }

        //public List<Locale> Locales { get; set; } = new List<Locale>();

        [JsonIgnore]
        public BsonDocument Details { get; set; } = new BsonDocument();

    }

    /*
    public class Locale
    {
        public string? Country { get; set; }
        public string? Language { get; set; }
    }
    */

    public class AppStoreRecordResult
    {
        public string? RecordId { get; set; }
        public string? Url { get; set; }
        public DateTime Retrieved { get; set; }
        public string? PrettifiedDetails { get; set; }
        public string? StoreCountry { get; set; }
        public string? StoreLanguage { get; set; }
    }

    public class AppStoreRecordDownloadResult
    {
        public string? AppId { get; set; }

        public DateTime Retrieved { get; set; }

        public dynamic? Details { get; set; }

        public string? StoreCountry { get; set; }
        public string? StoreLanguage { get; set; }

    }
}
