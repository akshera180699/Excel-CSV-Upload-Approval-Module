using Excel_CSVUploadAndApproval.Services;
using Excel_CSVUploadAndApproval.Repositories;
using Microsoft.Data.SqlClient;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped< Excel_CSVUploadAndApproval.Services.FileParseService>();
builder.Services.AddScoped<IFileParseService, FileParseService>();
builder.Services.AddScoped<IUploadService, UploadService>();

builder.Services.AddScoped<IUploadRepository, UploadRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}")
    pattern: "{controller=UploadFile}/{action=Index}/{id?}")
    .WithStaticAssets();



var connectionString =builder.Configuration.GetConnectionString("DefaultConnection  ");

try
{
    await using var connection = new SqlConnection( connectionString);
    await connection.OpenAsync();

    Console.WriteLine("SQL Server connection successful!");
    Console.WriteLine($"Connected database: {connection.Database}");
}
catch (Exception ex)
{
    Console.WriteLine($"Database connection failed: {ex.Message}");
}


app.Run();
