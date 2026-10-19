
using Microsoft.AspNetCore.Http;
using Excel_CSVUploadAndApproval.Models;

namespace Excel_CSVUploadAndApproval.Services
{
    public interface IUploadService
    {
        Task<FileUploadResult> ProcessAndSaveAsync(IFormFile? file);
        Task<UploadGridData?> GetGridAsync(int batchId);

        Task<bool> UpdateApprovalAsync(int batchId, int rowId,string approvalStatus);
    }
}
