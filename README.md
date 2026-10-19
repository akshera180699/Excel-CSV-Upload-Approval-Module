# Excel/CSV Upload & Approval Module

Web application .NET MVC web application connected to Microsoft SQL Server (MSSQL).
The system dynamically reads excel headers, which maps database schema dynamically, bulk-inserts records using SqlBulkCopy, and provides a real-time AJAX approval workflow.

# Key Features
- Multi-format Support: Upload `.xlsx`, `.xls`, and `.csv` files.
- Dynamic Schema Generation: Reads headers dynamically, handles duplicates & blanks, and sanitizes identifiers against SQL injection.
- High Performance: Utilizes `SqlBulkCopy` for streaming large datasets into MSSQL.
- Interactive UI: Dynamic column rendering in Razor view.
- Real-time AJAX Persistence: Update Approval Status (Approved / Not Approved) instantly without full page reload.

# Setup & Run Instructions
1. Open `appsettings.json` and configure your MSSQL Connection String.
2. Run `dotnet run` or press F5 in Visual Studio.git