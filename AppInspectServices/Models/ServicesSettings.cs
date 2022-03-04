namespace AppInspectServices.Models
{
    public class ServicesSettings
    {
        public List<string> WorkerAddresses { get; set; } = new List<string>();
        public string APKUploadPath { get; set; } = string.Empty;
        public string APKArchivePath { get; set; } = string.Empty;
        public string APKParquetPath { get; set; } = string.Empty;
       
    }
}
