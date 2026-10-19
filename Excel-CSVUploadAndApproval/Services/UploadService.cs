
using Microsoft.AspNetCore.Http;
using Excel_CSVUploadAndApproval.Models;
using Excel_CSVUploadAndApproval.Repositories;

namespace Excel_CSVUploadAndApproval.Services
{
    public class UploadService : IUploadService
    {
        private readonly IFileParseService _fileParseService;
        private readonly IUploadRepository _uploadRepository;

        public UploadService(IFileParseService fileParseService, IUploadRepository uploadRepository)
        {
            _fileParseService = fileParseService;
            _uploadRepository = uploadRepository;
        }

        #region Process and Save Upload

        public async Task<FileUploadResult> ProcessAndSaveAsync(IFormFile? file)
        {
            var result = _fileParseService.ProcessFile(file);

            if (!result.IsSuccess || result.Data == null)
                return result;

            try
            {

                var batchId = await _uploadRepository.SaveUploadAsync(file!.FileName, result.Data);

                result.Message = $"File uploaded and saved successfully. " + $"{result.Data.Rows.Count} rows saved.";

                result.BatchId = batchId;

                return result;
            }
            catch (Exception)
            {
                return new FileUploadResult
                {
                    IsSuccess = false,
                    Message =
                        "The file was read, but saving to the database failed."
                };
            }
        }

        #endregion

        public Task<UploadGridData?> GetGridAsync(int batchId)
        {
            return _uploadRepository.GetUploadGridAsync(batchId);
        }

        public Task<bool> UpdateApprovalAsync( int batchId,int rowId,string approvalStatus)
        {
            return _uploadRepository.UpdateApprovalAsync( batchId,  rowId, approvalStatus);
        }
    
}
}
