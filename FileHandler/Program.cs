using FileHandler.Features.GenerateTestFile.Services;
using FileHandler.Features.SortFile.Services;
using FileHandler.Infrastructure.Middlewares;
using FileHandler.Infrastructure.Options;
using FileHandler.Models;
using FileHandler.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

ConfigureServices();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();

void ConfigureServices()
{
    var services = builder.Services;
    var configuration = builder.Configuration;

    services.Configure<FileSortingOptions>(configuration.GetSection(FileSortingOptions.Key));

    services.AddSingleton<ITempDirectoryManager, TempDirectoryManager>();
    services.AddSingleton<IFileManager, FileManager>();
    services.AddSingleton<IComparer<LineEntry>, LineEntryComparer>();
    services.AddSingleton<IChunkMerger, KWayChunkMerger>();
    services.AddScoped<IFileSorter, FileSorter>();
    services.AddSingleton<ILineEntryGenerator, LineEntryGenerator>();
    services.AddScoped<ITestFileGenerator, TestFileGenerator>();
}