using ProjectPlanParser.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<IProjectParserService, ProjectParserService>();

// 1. Configure CORS for Front-end integration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string>() ?? "*";
        if (allowedOrigins == "*")
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        
        policy.AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Configure Response Compression for large JSON payloads
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// 3. Configure Health Checks for Container Apps
builder.Services.AddHealthChecks();

// 4. Optimize JSON Serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// 5. Configure Aspose.Tasks License
var licensePath = builder.Configuration["ASPOSE_LICENSE_PATH"];
// Default to /app/licenses/Aspose.Tasks.lic if not specified and the file exists
if (string.IsNullOrEmpty(licensePath))
{
    var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "licenses", "Aspose.Tasks.lic");
    if (File.Exists(defaultPath))
    {
        licensePath = defaultPath;
    }
}

var meteredPublicKey = builder.Configuration["ASPOSE_METERED_PUBLIC_KEY"];
var meteredPrivateKey = builder.Configuration["ASPOSE_METERED_PRIVATE_KEY"];

if (!string.IsNullOrEmpty(meteredPublicKey) && !string.IsNullOrEmpty(meteredPrivateKey))
{
    try
    {
        var metered = new Aspose.Tasks.Metered();
        metered.SetMeteredKey(meteredPublicKey, meteredPrivateKey);
        Console.WriteLine("Aspose.Tasks Metered License applied.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to apply Aspose.Tasks Metered License: {ex.Message}");
    }
}
else if (!string.IsNullOrEmpty(licensePath) && File.Exists(licensePath))
{
    try
    {
        var license = new Aspose.Tasks.License();
        license.SetLicense(licensePath);
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine($"[DevMode] Aspose.Tasks License file applied from: {licensePath}");
        }
        else
        {
            Console.WriteLine("Aspose.Tasks License file applied.");
        }
    }
    catch (Exception ex)
    {
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine($"Failed to apply Aspose.Tasks License from file ({licensePath}): {ex.Message}");
        }
        else
        {
            Console.WriteLine("Failed to apply Aspose.Tasks License from file.");
        }
    }
}

// Configure Kestrel limits
builder.WebHost.ConfigureKestrel(options =>
{
    // Max request body size (e.g., 100MB)
    options.Limits.MaxRequestBodySize = 104857600; 
});

var app = builder.Build();

// Use Response Compression
app.UseResponseCompression();

// Use CORS
app.UseCors();

// Map Health Checks
app.MapHealthChecks("/health");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/parse", async (HttpContext context,
    IProjectParserService parserService, 
    ILogger<Program> logger,
    [FromQuery] bool? includeProjectProperties,
    [FromQuery] bool? includeTasks,
    [FromQuery] bool? includeResources,
    [FromQuery] bool? includeAssignments,
    [FromQuery] bool? includeCalendars,
    [FromQuery] bool? includeTaskLinks,
    [FromQuery] bool? includeTimephased,
    [FromQuery] bool? includeBaselines,
    [FromQuery] bool? includeExtendedAttributes) =>
{
    var request = context.Request;
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Invalid content type. Expected multipart/form-data.");
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");

    if (file == null || file.Length == 0)
    {
        logger.LogWarning("Parse request received with no file or empty file.");
        return Results.BadRequest("No file uploaded.");
    }

    if (!Path.GetExtension(file.FileName).Equals(".mpp", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogWarning("Invalid file extension: {FileName}", file.FileName);
        return Results.BadRequest("Only .mpp files are supported.");
    }

    // Validate file header (Magic Bytes)
    // .mpp files are usually OLE2 Compound Document files
    // Signature: D0 CF 11 E0 A1 B1 1A E1
    byte[] ole2Signature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
    
    try
    {
        using (var headerStream = file.OpenReadStream())
        {
            byte[] header = new byte[8];
            int bytesRead = headerStream.Read(header, 0, 8);
            if (bytesRead < 8 || !header.SequenceEqual(ole2Signature))
            {
                logger.LogWarning("Invalid file header for file: {FileName}", file.FileName);
                return Results.BadRequest("Invalid file header. The file does not appear to be a valid Microsoft Project file.");
            }
        }

        var options = new ParseOptions
        {
            IncludeProjectProperties = includeProjectProperties ?? true,
            IncludeTasks = includeTasks ?? true,
            IncludeResources = includeResources ?? true,
            IncludeAssignments = includeAssignments ?? true,
            IncludeCalendars = includeCalendars ?? true,
            IncludeTaskLinks = includeTaskLinks ?? true,
            IncludeTimephasedData = includeTimephased ?? true,
            IncludeBaselines = includeBaselines ?? true,
            IncludeExtendedAttributes = includeExtendedAttributes ?? true
        };

        logger.LogInformation("Processing file: {FileName} ({Size} bytes)", file.FileName, file.Length);
        using var stream = file.OpenReadStream();
        var result = parserService.ParseProject(stream, options);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while parsing the project file: {FileName}", file.FileName);
        
        if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return Results.Problem($"An error occurred while parsing the project: {ex.Message}. StackTrace: {ex.StackTrace}");
        }
        
        return Results.Problem("An error occurred while parsing the project. Please ensure the file is a valid .mpp file and try again.");
    }
})
.WithName("ParseProjectFile")
.WithOpenApi();

app.Run();

public partial class Program { }