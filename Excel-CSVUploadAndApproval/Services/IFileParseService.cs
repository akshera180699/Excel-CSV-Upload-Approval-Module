using Microsoft.AspNetCore.Http;
using Excel_CSVUploadAndApproval.Models;

namespace Excel_CSVUploadAndApproval.Services
{
    public interface IFileParseService
    {
        FileUploadResult ProcessFile(IFormFile? file);

        ParseUpload Parse(IFormFile file);
    }
}