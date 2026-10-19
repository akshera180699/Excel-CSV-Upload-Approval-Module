
using Microsoft.AspNetCore.Mvc;
using Excel_CSVUploadAndApproval.Models;
using Excel_CSVUploadAndApproval.Services;
using Excel_CSVUploadAndApproval.ViewModels;

namespace Excel_CSVUploadAndApproval.Controllers
{
    public class UploadFileController : Controller
    {
        private readonly IUploadService _uploadService;

        public UploadFileController(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        
        [HttpGet]
        public IActionResult Index()
        {
            return View(new UploadFileViewModel());
        }

        // POST: /UploadFile/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UploadFileViewModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                model.IsSuccess = false;
                model.Message = "Please select a valid file to upload.";
                return View(model);
            }

            var result = await _uploadService.ProcessAndSaveAsync(model.File);

            model.IsSuccess = result.IsSuccess;
            model.Message = result.Message;

            if (result.IsSuccess && result.BatchId.HasValue)
            {
                return RedirectToAction(nameof(Grid), new { batchId = result.BatchId.Value });
            }

            return View(model);
        }

        
        [HttpGet]
        public async Task<IActionResult> Grid(int batchId)
        {
            if (batchId <= 0)
            {
                return BadRequest("Invalid upload batch ID.");
            }

            var data = await _uploadService.GetGridAsync(batchId);

            if (data == null)
            {
                return NotFound("Uploaded data was not found.");
            }

            return View(data);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApproval(int batchId, int rowId, string approvalStatus)
        {
            if (batchId <= 0 || rowId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid batch or row ID." });
            }

            if (string.IsNullOrWhiteSpace(approvalStatus) || (approvalStatus != "Approved" && approvalStatus != "Not Approved"))
            {
                return BadRequest(new { success = false, message = "Invalid approval status." });
            }

            var updated = await _uploadService.UpdateApprovalAsync(batchId, rowId, approvalStatus);

            if (!updated)
            {
                return NotFound(new { success = false, message = "The row was not found or its status could not be updated." });
            }

            return Json(new { success = true, message = "Approval status saved successfully." });
        }
    }
}
