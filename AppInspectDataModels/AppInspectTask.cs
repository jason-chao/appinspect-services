using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AppInspectDataModels
{
    public class AppInspectTask
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public int BasePriority { get; set; } = 0;
        public string? Results { get; set; } 
        public string? Results_SHA256 { get; set; }
        public string? Error { get; set; }
        public string? Requester { get; set; } 
        public string? RequesterConnectionId { get; set; }
        public string? Worker { get; set; }
        public string? WorkerConnectionId { get; set; } 
        public DateTime Created { get;set; }
        public DateTime? Started { get; set; }
        public DateTime? Completed { get; set; }
        public DateTime? ResultsHandled { get; set; }
        public string? User { get; set; }
        public bool Public { get; set; } = true;
    }
}
