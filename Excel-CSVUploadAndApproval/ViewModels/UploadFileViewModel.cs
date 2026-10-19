
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Excel_CSVUploadAndApproval.ViewModels
{
    public class UploadFileViewModel
    {
        [Required(ErrorMessage = "Please select a file.")]
        public IFormFile? File { get; set; }

        public string? Message { get; set; }

        public bool IsSuccess { get; set; }

        public List<string> Headers { get; set; } = new();

        public List<List<string>> Rows { get; set; } = new();
    }
}
