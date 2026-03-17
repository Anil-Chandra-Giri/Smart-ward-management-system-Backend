using Tesseract;
using System.Text.RegularExpressions;

namespace OCR.Services
{
    public class OCRService
    {
        public string RunOCR(string imagePath)
        {
            using var engine = new TesseractEngine(@"./tessdata", "nep+eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);

            var text = page.GetText();

            // Clean OCR noise
            text = Regex.Replace(text, @"[A-Za-z]{1}\s[A-Za-z]{1}", "");
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }
    }
}