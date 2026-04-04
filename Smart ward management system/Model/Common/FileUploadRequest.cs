namespace OCR.Models
{
    public class FileUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string? DocumentSide { get; set; }
    }
}
