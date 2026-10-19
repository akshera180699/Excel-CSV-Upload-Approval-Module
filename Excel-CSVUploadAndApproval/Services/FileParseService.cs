
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Excel_CSVUploadAndApproval.Models;

namespace Excel_CSVUploadAndApproval.Services
{
    public class FileParseService : IFileParseService
    {
        #region File Validation and Processing

        public FileUploadResult ProcessFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return new FileUploadResult
                {
                    Message = "Please select a file."
                };
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                return new FileUploadResult
                {
                    Message = "File size cannot exceed 10 MB."
                };
            }

            try
            {
                var parsed = Parse(file);

                if (parsed.Headers.Count == 0)
                {
                    return new FileUploadResult
                    {
                        Message = "The file must contain a header row."
                    };
                }

                return new FileUploadResult
                {
                    IsSuccess = true,
                    Message = $"File read successfully. {parsed.Rows.Count} data rows found.",
                    Data = parsed
                };
            }
            catch (InvalidOperationException ex)
            {
                return new FileUploadResult
                {
                    Message = ex.Message
                };
            }
            catch (Exception)
            {
                return new FileUploadResult
                {
                    Message = "Unable to read this file. Please check its format and try again."
                };
            }
        }

        #endregion

        #region File Format Detection

        public ParseUpload Parse(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("The file is empty.");

            var extension = Path.GetExtension(file.FileName) .ToLowerInvariant();

            if (extension == ".csv")
                return ParseCsv(file);

            if (extension == ".xlsx" || extension == ".xls")
                return ParseExcel(file);

            throw new InvalidOperationException(
                "Only .xlsx, .xls and .csv files are supported.");
        }

        #endregion

        #region CSV Parsing

        private ParseUpload ParseCsv(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(
                reader, CultureInfo.InvariantCulture);

            if (!csv.Read())
                throw new InvalidOperationException(
                    "The file has no header row.");

            csv.ReadHeader();
            var rawHeaders = csv.HeaderRecord;

            if (rawHeaders == null ||
                rawHeaders.Length == 0 ||
                rawHeaders.All(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException(
                    "The file has no header row.");
            }

            var result = new ParseUpload
            {
                Headers = CleanHeaders(rawHeaders)
            };

            while (csv.Read())
            {
                var row = new List<string>();

                for (int i = 0; i < rawHeaders.Length; i++)
                    row.Add(csv.GetField(i) ?? string.Empty);

                if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
                    result.Rows.Add(row);
            }

            return result;
        }

        #endregion

        #region Excel Parsing

        private ParseUpload ParseExcel(IFormFile file)
        {
            Encoding.RegisterProvider( CodePagesEncodingProvider.Instance);

            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);

            if (!reader.Read())
                throw new InvalidOperationException(
                    "The file has no header row.");

            var rawHeaders = new string[reader.FieldCount];

            for (int i = 0; i < reader.FieldCount; i++)
            {
                rawHeaders[i] = Convert.ToString(reader.GetValue(i)) ?? string.Empty;
            }

            if (rawHeaders.All(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException(
                    "The file has no header row.");

            var result = new ParseUpload
            {
                Headers = CleanHeaders(rawHeaders)
            };

            while (reader.Read())
            {
                var row = new List<string>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(Convert.ToString(  reader.GetValue(i), CultureInfo.InvariantCulture) ?? string.Empty);
                }

                if (row.Any(value => !string.IsNullOrWhiteSpace(value)))
                    result.Rows.Add(row);
            }

            return result;
        }

        #endregion

        #region Header Sanitization

        private List<string> CleanHeaders(IEnumerable<string> rawHeaders)
        {
            var headers = new List<string>();
            var usedNames = new HashSet<string>( StringComparer.OrdinalIgnoreCase);

            int columnNumber = 0;

            foreach (var raw in rawHeaders)
            {
                columnNumber++;

                var name = Regex.Replace( (raw ?? string.Empty).Trim(), @"[^a-zA-Z0-9_]", "_");

                if (string.IsNullOrWhiteSpace(name))
                    name = $"Column_{columnNumber}";

                if (char.IsDigit(name[0]))
                    name = $"Column_{name}";

                var baseName = name;
                int suffix = 2;

                while (!usedNames.Add(name))
                    name = $"{baseName}_{suffix++}";

                headers.Add(name);
            }

            return headers;
        }

        #endregion
    }
}
