using System.Text.Json.Serialization;

namespace AppInspectDataModels
{
    public class AppStoreQueryRecord
    {
        [JsonPropertyName("appId")]
        public string? AppId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("developer")]
        public string? Developer { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("comments")]
        public List<string>? Comments { get; set; }

        [JsonPropertyName("version")]
        public string? VersionName { get; set; }

        [JsonPropertyName("updated")]
        public long? Updated { get; set; }

        [JsonPropertyName("released")]
        public string? Released { get; set; }

        [JsonPropertyName("minInstalls")]
        public long? MinInstalls { get; set; }

        [JsonPropertyName("maxInstalls")]
        public long? MaxInstalls { get; set; }

        [JsonPropertyName("adSupported")]
        public bool? AdSupported { get; set; }

        [JsonPropertyName("contentRating")]
        public string? ContentRating { get; set; }

        [JsonPropertyName("scoreText")]
        public string? Score { get; set; }

        [JsonPropertyName("priceText")]
        public string? Price { get; set; }

        [JsonPropertyName("recentChanges")]
        public string? RecentChanges { get; set; }
    }

    public class AppStoreRecordMetadata
    {
        public DateTime Retrieved { get; set; }
        public string? StoreLocale_Country { get; set; }
        public string? StoreLocale_Language { get; set; }
    }

    /*
    public class AppDetailsCSVRow : AppStoreQueryRecord
    {
        public AppDetailsCSVRow(AppStoreQueryRecord baseRecord)
        {
            var properties = baseRecord.GetType().GetProperties();

            properties.ToList().ForEach(property =>
            {
                var isPresent = this.GetType().GetProperty(property.Name);
                if (isPresent != null && property.CanWrite)
                {
                    var value = baseRecord.GetType().GetProperty(property.Name)!.GetValue(baseRecord, null);

                    if (value != null)
                    {
                        var targetProperty = this.GetType().GetProperty(property.Name);

                        if (targetProperty != null)
                        {
                            targetProperty.SetValue(this, value, null);
                        }
                    }
                }
            });
        }

        public DateTime Retrieved { get; set; }
        public string? StoreLocale_Country { get; set; }
        public string? StoreLocale_Language { get; set; }
    }
    */

    /*
    public class AppStoreQueryRecordCSVRow : AppStoreQueryRecord
    {
        public AppStoreQueryRecordCSVRow(AppStoreQueryRecord baseRecord)
        {
            var properties = baseRecord.GetType().GetProperties();

            properties.ToList().ForEach(property =>
            {
                var isPresent = this.GetType().GetProperty(property.Name);
                if (isPresent != null && property.CanWrite)
                {
                    var value = baseRecord.GetType().GetProperty(property.Name)!.GetValue(baseRecord, null);

                    if (value != null)
                    {
                        var targetProperty = this.GetType().GetProperty(property.Name);

                        if (targetProperty != null)
                        {
                            targetProperty.SetValue(this, value, null);
                        }
                    }
                }
            });
        }

        public string TaskId { get; set; } = string.Empty;
        public DateTime? Executed { get; set; }
        public string? QueryArguments { get; set; }
    }*/
}
