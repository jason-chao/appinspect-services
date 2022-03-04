using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AppInspectDataModels
{
    public class AppFileRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = string.Empty;

        public string AppId { get; set; } = string.Empty;
        public string SHA256 { get; set; } = string.Empty;
        public long VersionCode { get; set; } = -1;
        public string VersionName { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public string? APKBaseFilenameInArchive { get; set; }
        public string? ParquetBasename { get; set; }
        public bool? SignatureVerified { get; set; }
        public string? SignedByCertificate { get; set; }
    }
}
