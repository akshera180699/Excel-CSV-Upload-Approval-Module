namespace Excel_CSVUploadAndApproval.Models
{
    public class ParseUpload
    {
        public List<string> Headers { get; set; } = new();

        public List<List<string>> Rows { get; set; } = new();
    }
}
