namespace WebApplication1.Services
{
    public interface IDataMigrationService
    {
        Task<DataMigrationResult> EncodeExistingAddressFieldsAsync();
    }

    public class DataMigrationResult
    {
        public bool Success { get; set; }
        public int RecordsProcessed { get; set; }
        public int RecordsUpdated { get; set; }
        public int RecordsFailed { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}
