
using Excel_CSVUploadAndApproval.Models;

namespace Excel_CSVUploadAndApproval.Repositories
{
    public interface IUploadRepository
    {
        Task<int> SaveUploadAsync( string fileName, ParseUpload data);

        Task<UploadGridData?> GetUploadGridAsync( int batchId);

        Task<bool> UpdateApprovalAsync( int batchId, int rowId, string approvalStatus);
    }
}
