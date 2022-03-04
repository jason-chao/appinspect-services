namespace AppInspectDataModels
{
    public class InArchiveCheckResults
    {
        public IEnumerable<string> ParquetFilenames { get; set; } = new List<string>();
        public IEnumerable<string> AppIdsNotInArchive { get; set;} = new List<string>();
    }
}
