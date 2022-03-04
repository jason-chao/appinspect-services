namespace AppInspectDataModels
{
    public class TaskInfoCSVRow
    {
        public string TaskId { get; set; } = string.Empty;
        public DateTime? Executed { get; set; }
        public string? QueryArguments { get; set; }
    }
}
