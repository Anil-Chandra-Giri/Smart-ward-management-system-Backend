using Microsoft.AspNetCore.Mvc;
using OCR.Services;
using OCR.Models;
using Smart_ward_management_system.Services;

[ApiController]
[Route("api/[controller]")]
public class OcrController : ControllerBase
{
    private readonly OCRService _ocrService;
    private readonly FieldExtractorService _extractor;
    private readonly PreprocessorService _preprocessor;

    public OcrController()
    {
        _ocrService = new OCRService();
        _extractor = new FieldExtractorService();
        _preprocessor = new PreprocessorService();
    }

    [HttpPost("scan")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Scan([FromForm] FileUploadRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("No file uploaded.");

        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
        var filePath = Path.Combine(uploadFolder, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await request.File.CopyToAsync(stream);

        //string processedPath = _preprocessor.PreprocessImage(filePath);

        var ocrText = _ocrService.RunOCR(filePath);
        var result = _extractor.ExtractFields(ocrText);

        return Ok(new
        {
            success = true,
            documentType = result.documentType,
            fields = result.fields,
            rawText = ocrText
        });
    }
}