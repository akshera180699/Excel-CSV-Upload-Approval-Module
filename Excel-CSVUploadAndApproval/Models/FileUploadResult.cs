
namespace Excel_CSVUploadAndApproval.Models
{
    public class FileUploadResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public ParseUpload? Data { get; set; }
        public int? BatchId { get; set; }
    }
}
