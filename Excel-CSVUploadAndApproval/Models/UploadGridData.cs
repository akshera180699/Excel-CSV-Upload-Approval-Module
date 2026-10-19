
namespace Excel_CSVUploadAndApproval.Models
{
    public class UploadGridData
    {
        public int BatchId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public List<string> Headers { get; set; } = new();

        public List<Dictionary<string, string?>> Rows { get; set; } = new();
    }
}
