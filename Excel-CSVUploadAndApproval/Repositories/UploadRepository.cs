
using System.Data;
using Excel_CSVUploadAndApproval.Models;
using Microsoft.Data.SqlClient;

namespace Excel_CSVUploadAndApproval.Repositories
{
    public class UploadRepository : IUploadRepository
    {
        private readonly string _connectionString;

        public UploadRepository(IConfiguration configuration)
        {
            _connectionString =configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Database connection string is missing.");
        }

        #region Save Upload

        public async Task<int> SaveUploadAsync(string fileName, ParseUpload data)
        {
            if (data.Headers.Count == 0)
                throw new InvalidOperationException( "The uploaded file has no columns.");

            var headers = PrepareHeaders(data.Headers);

            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            try
            {
                int batchId;

                const string insertBatchSql = """
                    INSERT INTO dbo.UploadBatches
                        (OriginalFileName, TableName)
                    VALUES
                        (@FileName, @TableName);

                    SELECT CAST(SCOPE_IDENTITY() AS INT);
                    """;

                await using (var command = new SqlCommand( insertBatchSql, connection, transaction))
                {
                    command.Parameters.Add( "@FileName", SqlDbType.NVarChar, 255).Value = Path.GetFileName(fileName);

                    command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = "Pending";

                    batchId = Convert.ToInt32( await command.ExecuteScalarAsync());
                }

                var tableName = $"UploadData_{batchId}";

                var columnDefinitions = string.Join(
                    ", ",
                    headers.Select(header =>
                        $"{QuoteIdentifier(header)} NVARCHAR(MAX) NULL"));

                var createTableSql = $"""
                    CREATE TABLE dbo.{QuoteIdentifier(tableName)}
                    (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        {columnDefinitions},
                        [ApprovalStatus] NVARCHAR(20) NOT NULL
                            DEFAULT 'Not Approved'
                    );
                    """;

                await using (var command = new SqlCommand( createTableSql, connection, transaction))
                {
                    await command.ExecuteNonQueryAsync();
                }

                const string updateBatchSql = """
                    UPDATE dbo.UploadBatches
                    SET TableName = @TableName
                    WHERE BatchId = @BatchId;
                    """;

                await using (var command = new SqlCommand( updateBatchSql, connection, transaction))
                {
                    command.Parameters.Add( "@TableName", SqlDbType.NVarChar, 128).Value =tableName;

                    command.Parameters.Add( "@BatchId", SqlDbType.Int).Value = batchId;

                    await command.ExecuteNonQueryAsync();
                }

                var dataTable = new DataTable();

                foreach (var header in headers)
                    dataTable.Columns.Add(header, typeof(string));

                foreach (var row in data.Rows)
                {
                    var dataRow = dataTable.NewRow();

                    for (int i = 0; i < headers.Count; i++)
                    {
                        dataRow[i] = i < row.Count
                            ? row[i] ?? string.Empty
                            : string.Empty;
                    }

                    dataTable.Rows.Add(dataRow);
                }

                using (var bulkCopy = new SqlBulkCopy(  connection, SqlBulkCopyOptions.CheckConstraints, transaction))
                {
                    bulkCopy.DestinationTableName = $"dbo.{QuoteIdentifier(tableName)}";

                    foreach (var header in headers)
                        bulkCopy.ColumnMappings.Add(header, header);

                    bulkCopy.BatchSize = 1000;
                    bulkCopy.BulkCopyTimeout = 120;

                    await bulkCopy.WriteToServerAsync(dataTable);
                }

                await transaction.CommitAsync();

                return batchId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Retrieve Upload Grid

        public async Task<UploadGridData?> GetUploadGridAsync( int batchId)
        {
            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            string? tableName;
            string? fileName;

            const string batchSql = """
                SELECT TableName, OriginalFileName
                FROM dbo.UploadBatches
                WHERE BatchId = @BatchId;
                """;

            await using (var command = new SqlCommand(  batchSql, connection))
            {
                command.Parameters.Add( "@BatchId", SqlDbType.Int).Value = batchId;

                await using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                tableName = reader.GetString(0);
                fileName = reader.GetString(1);
            }

            if (!IsValidTableName(tableName, batchId))
                throw new InvalidOperationException(
                    "The upload table name is invalid.");

            var result = new UploadGridData
            {
                BatchId = batchId,
                FileName = fileName ?? string.Empty
            };

            var sql = $"""
                SELECT *
                FROM dbo.{QuoteIdentifier(tableName!)}
                ORDER BY [Id];
                """;

            await using (var command = new SqlCommand(sql, connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                    result.Headers.Add(reader.GetName(i));

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, string?>(
                        StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] =
                            reader.IsDBNull(i)
                                ? null
                                : Convert.ToString(reader.GetValue(i));
                    }

                    result.Rows.Add(row);
                }
            }

            return result;
        }

        #endregion

        #region Update Approval

        public async Task<bool> UpdateApprovalAsync( int batchId, int rowId, string approvalStatus)
        {
            if (approvalStatus != "Approved" && approvalStatus != "Not Approved")
            {
                throw new ArgumentException( "Invalid approval status.", nameof(approvalStatus));
            }

            await using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            string? tableName;

            const string batchSql = """
                SELECT TableName
                FROM dbo.UploadBatches
                WHERE BatchId = @BatchId;
                """;

            await using (var command = new SqlCommand( batchSql, connection))
            {
                command.Parameters.Add(  "@BatchId", SqlDbType.Int).Value = batchId;

                var value = await command.ExecuteScalarAsync();

                if (value == null || value == DBNull.Value)
                    return false;

                tableName = Convert.ToString(value);
            }

            if (!IsValidTableName(tableName, batchId))
                throw new InvalidOperationException(
                    "The upload table name is invalid.");

            var updateSql = $"""
                UPDATE dbo.{QuoteIdentifier(tableName!)}
                SET [ApprovalStatus] = @Status
                WHERE [Id] = @RowId;
                """;

            await using var updateCommand = new SqlCommand(
                updateSql, connection);

            updateCommand.Parameters.Add( "@Status", SqlDbType.NVarChar, 20).Value = approvalStatus;

            updateCommand.Parameters.Add( "@RowId", SqlDbType.Int).Value = rowId;

            return await updateCommand.ExecuteNonQueryAsync() == 1;
        }

        #endregion

        #region Header and Identifier Safety

        private static List<string> PrepareHeaders(
            IEnumerable<string> sourceHeaders)
        {
            var result = new List<string>();
            var used = new HashSet<string>( StringComparer.OrdinalIgnoreCase)
            {
                "Id",
                "ApprovalStatus"
            };

            int index = 0;

            foreach (var source in sourceHeaders)
            {
                index++;

                var name = source.Trim();

                if (string.IsNullOrWhiteSpace(name))
                    name = $"Column_{index}";

                var baseName = name;
                int suffix = 2;

                while (!used.Add(name))
                    name = $"{baseName}_{suffix++}";

                result.Add(name);
            }

            return result;
        }

        private static string QuoteIdentifier(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private static bool IsValidTableName( string? tableName, int batchId)
        {
            return string.Equals(tableName, $"UploadData_{batchId}",   StringComparison.Ordinal);
        }

        #endregion
    }
}
